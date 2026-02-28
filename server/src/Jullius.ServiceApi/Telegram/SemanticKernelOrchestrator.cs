#pragma warning disable SKEXP0070 // Google AI connector is experimental

using Jullius.ServiceApi.Application.Services;
using Jullius.ServiceApi.Telegram.Plugins;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Google;

namespace Jullius.ServiceApi.Telegram;

/// <summary>
/// Orquestrador central baseado em Semantic Kernel.
/// Substitui o antigo ConversationOrchestrator + GeminiAssistantService.
/// O LLM decide autonomamente quais funções invocar via FunctionChoiceBehavior.Auto().
/// </summary>
public sealed class SemanticKernelOrchestrator
{
    private const string GeminiModel = "gemini-3-flash-preview";

    private readonly ChatHistoryStore _chatHistoryStore;
    private readonly BotConfigurationService _configService;
    private readonly IServiceProvider _serviceProvider;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<SemanticKernelOrchestrator> _logger;

    public SemanticKernelOrchestrator(
        ChatHistoryStore chatHistoryStore,
        BotConfigurationService configService,
        IServiceProvider serviceProvider,
        IHttpClientFactory httpClientFactory,
        ILogger<SemanticKernelOrchestrator> logger)
    {
        _chatHistoryStore = chatHistoryStore;
        _configService = configService;
        _serviceProvider = serviceProvider;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    /// <summary>
    /// Processa uma mensagem de texto do usuário.
    /// </summary>
    public async Task<string> ProcessMessageAsync(long chatId, string message)
    {
        try
        {
            var kernel = await BuildKernelAsync();
            var history = PrepareHistory(chatId);

            history.AddUserMessage(message);
            _chatHistoryStore.AddUserMessage(chatId, message);

            var response = await ExecuteChatAsync(kernel, history);

            _chatHistoryStore.AddAssistantMessage(chatId, response);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao processar mensagem de texto. ChatId: {ChatId}", chatId);
            return "❌ Ocorreu um erro ao processar sua mensagem. Tente novamente em breve.";
        }
    }

    /// <summary>
    /// Processa uma mensagem com mídia (foto, áudio).
    /// O conteúdo binário é enviado ao modelo como parte da mensagem.
    /// </summary>
    public async Task<string> ProcessMediaMessageAsync(long chatId, byte[] mediaBytes, string mimeType, string? caption)
    {
        try
        {
            var kernel = await BuildKernelAsync();
            var history = PrepareHistory(chatId);

            // Construir mensagem multimodal com conteúdo binário + texto opcional
            var items = new ChatMessageContentItemCollection();

            if (!string.IsNullOrWhiteSpace(caption))
            {
                items.Add(new TextContent(caption));
            }

            var mediaDescription = mimeType.StartsWith("audio/", StringComparison.OrdinalIgnoreCase)
                ? "Áudio enviado pelo usuário"
                : "Imagem enviada pelo usuário";

            if (string.IsNullOrWhiteSpace(caption))
            {
                items.Add(new TextContent($"[{mediaDescription}] — Analise o conteúdo e identifique transações financeiras."));
            }

            items.Add(new ImageContent(mediaBytes, mimeType));

            var userMessage = new ChatMessageContent(AuthorRole.User, items);
            history.Add(userMessage);

            // Adicionar descrição textual ao store (ele não suporta binário)
            _chatHistoryStore.AddUserMessage(chatId, $"[{mediaDescription}] {caption ?? ""}".Trim());

            var response = await ExecuteChatAsync(kernel, history);

            _chatHistoryStore.AddAssistantMessage(chatId, response);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao processar mensagem de mídia. ChatId: {ChatId}", chatId);
            return "❌ Ocorreu um erro ao processar a mídia. Tente novamente em breve.";
        }
    }

    /// <summary>
    /// Constrói um Kernel fresco com o API key atual e todos os plugins registrados.
    /// </summary>
    private async Task<Kernel> BuildKernelAsync()
    {
        var apiKey = await _configService.GetDecryptedValueAsync("GeminiApiKey");
        if (string.IsNullOrEmpty(apiKey))
            throw new InvalidOperationException("GeminiApiKey não configurada no banco.");

        var builder = Kernel.CreateBuilder();

        // Usar HttpClient com timeout estendido (5 min) para Thinking models.
        // O default de 100s causa TaskCanceledException após function calling chains longas.
        var httpClient = _httpClientFactory.CreateClient("GeminiApi");

        builder.AddGoogleAIGeminiChatCompletion(
            modelId: GeminiModel,
            apiKey: apiKey,
            httpClient: httpClient);

        // Registrar plugins resolvendo do container DI
        builder.Plugins.AddFromObject(_serviceProvider.GetRequiredService<DateTimePlugin>(), "DateTime");
        builder.Plugins.AddFromObject(_serviceProvider.GetRequiredService<FinancialTransactionPlugin>(), "FinancialTransaction");
        builder.Plugins.AddFromObject(_serviceProvider.GetRequiredService<CardTransactionPlugin>(), "CardTransaction");
        builder.Plugins.AddFromObject(_serviceProvider.GetRequiredService<CategoryPlugin>(), "Category");
        builder.Plugins.AddFromObject(_serviceProvider.GetRequiredService<BudgetPlugin>(), "Budget");

        var kernel = builder.Build();

        // Registrar o filtro de invocação de funções
        var filter = _serviceProvider.GetRequiredService<FinancialFunctionFilter>();
        kernel.FunctionInvocationFilters.Add(filter);

        return kernel;
    }

    /// <summary>
    /// Prepara o ChatHistory adicionando o system prompt se ausente.
    /// </summary>
    private ChatHistory PrepareHistory(long chatId)
    {
        var history = _chatHistoryStore.GetOrCreate(chatId);

        // Garantir que o system prompt está presente
        if (history.Count == 0 || history[0].Role != AuthorRole.System)
        {
            history.Insert(0, new ChatMessageContent(AuthorRole.System, SystemPrompts.FinancialAssistant));
        }

        return history;
    }

    /// <summary>
    /// Executa a chamada ao modelo com auto function calling habilitado.
    /// </summary>
    private async Task<string> ExecuteChatAsync(Kernel kernel, ChatHistory history)
    {
        var chatService = kernel.GetRequiredService<IChatCompletionService>();

        var settings = new GeminiPromptExecutionSettings
        {
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(),
            Temperature = 0.7,
            TopP = 0.9
            // MaxTokens omitido intencionalmente: com Thinking models (Gemini 3 Flash),
            // tokens de raciocínio consomem o budget de MaxTokens. Deixar null permite
            // que o modelo use seu default, evitando respostas vazias (SK Issue #12334).
        };

        var result = await chatService.GetChatMessageContentAsync(history, settings, kernel);

        _logger.LogDebug(
            "Gemini raw response — ModelId={ModelId}, Role={Role}, Content IsNull={IsNull}, ContentLength={Length}, ItemsCount={Items}, MetadataKeys={MetaKeys}",
            result.ModelId,
            result.Role,
            result.Content is null,
            result.Content?.Length ?? -1,
            result.Items?.Count ?? 0,
            result.Metadata is not null ? string.Join(",", result.Metadata.Keys) : "(none)");

        // SEMPRE extrair dos Items filtrando conteúdo de pensamento (Thinking models).
        // result.Content concatena TODOS os TextContent (incluindo thinking), o que causa
        // duplicação. Extraímos manualmente apenas os itens que NÃO são pensamento.
        string? responseText = null;

        if (result.Items is { Count: > 0 })
        {
            var textItems = result.Items.OfType<TextContent>().ToList();

            // Filtrar items de pensamento se houver metadata de thinking
            var nonThinkingItems = textItems
                .Where(t => t.Metadata is null ||
                    (!t.Metadata.ContainsKey("IsThinking") && !t.Metadata.ContainsKey("IsThought")))
                .ToList();

            _logger.LogDebug(
                "TextContent items: Total={Total}, NonThinking={NonThinking}",
                textItems.Count,
                nonThinkingItems.Count);

            responseText = string.Join("", nonThinkingItems.Select(t => t.Text));
        }

        // Fallback para result.Content quando Items está vazio (modelos que não usam thinking)
        if (string.IsNullOrWhiteSpace(responseText))
        {
            responseText = result.Content;
        }

        if (string.IsNullOrWhiteSpace(responseText))
        {
            _logger.LogWarning(
                "Gemini retornou resposta vazia. Content IsNull={IsNull}, Content='{Content}', ModelId={ModelId}, Items={ItemTypes}",
                result.Content is null,
                result.Content ?? "(null)",
                result.ModelId,
                result.Items is not null
                    ? string.Join(",", result.Items.Select(i => i.GetType().Name))
                    : "(none)");

            responseText = "🤔 Não obtive resposta. Tente reformular sua mensagem.";
        }

        // Adicionar resposta ao histórico real do SK (para function-calling chain)
        history.Add(result);

        return responseText;
    }
}
