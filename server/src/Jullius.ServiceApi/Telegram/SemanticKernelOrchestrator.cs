#pragma warning disable SKEXP0070 // Google AI connector is experimental

using System.Net;
using System.Text.RegularExpressions;
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
public sealed partial class SemanticKernelOrchestrator
{
    private const string GeminiModel = "gemini-3-flash-preview";
    private const int MaxLoggedPayloadLength = 2000;

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

            // Adiciona a mensagem do usuário UMA ÚNICA VEZ ao ChatHistory compartilhado.
            // PrepareHistory retorna o mesmo objeto gerenciado pelo ChatHistoryStore,
            // então NÃO devemos chamar _chatHistoryStore.AddUserMessage — isso duplicaria
            // a mensagem e faria o LLM gerar respostas duplicadas.
            history.AddUserMessage(message);

            var response = await ExecuteChatAsync(kernel, history);

            // ExecuteChatAsync já adiciona o result ao history via history.Add(result).
            // Não chamar _chatHistoryStore.AddAssistantMessage para evitar duplicação.
            _chatHistoryStore.TrimHistory(chatId);

            return response;
        }
        catch (HttpOperationException ex)
        {
            LogGeminiHttpFailure(ex, chatId, "text");
            return BuildGeminiFailureMessage(ex.StatusCode);
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
            // Adiciona a mensagem multimodal UMA ÚNICA VEZ ao ChatHistory compartilhado.
            // PrepareHistory retorna o mesmo objeto do ChatHistoryStore — chamar
            // _chatHistoryStore.AddUserMessage adicionaria uma segunda mensagem (texto puro)
            // ao mesmo history, fazendo o LLM ver duas mensagens do usuário e potencialmente
            // duplicar a resposta.
            history.Add(userMessage);

            var response = await ExecuteChatAsync(kernel, history);

            // ExecuteChatAsync já adiciona o result ao history.
            _chatHistoryStore.TrimHistory(chatId);

            return response;
        }
        catch (HttpOperationException ex)
        {
            LogGeminiHttpFailure(ex, chatId, "media");
            return BuildGeminiFailureMessage(ex.StatusCode);
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

        _logger.LogInformation(
            "Gemini response — ModelId={ModelId}, Role={Role}, ContentLength={Length}, ItemsCount={Items}, ItemTypes={ItemTypes}",
            result.ModelId,
            result.Role,
            result.Content?.Length ?? -1,
            result.Items?.Count ?? 0,
            result.Items is not null
                ? string.Join(", ", result.Items.Select(i => i.GetType().Name))
                : "(none)");

        // O connector Google 1.72.0-alpha usa part.Thought==true e separa conteúdo de
        // pensamento em ReasoningContent (NÃO TextContent). O texto regular vai para
        // result.Content via o campo _content no construtor.
        //
        // NÃO extrair via Items.OfType<TextContent>() porque o Items pode conter
        // TextContent duplicados (um do construtor + um adicionado pelo connector),
        // causando duplicação ao concatenar.
        var responseText = result.Content;

        if (string.IsNullOrWhiteSpace(responseText))
        {
            _logger.LogWarning(
                "Gemini retornou resposta vazia. ContentLength={Length}, ModelId={ModelId}, ItemTypes={ItemTypes}",
                result.Content?.Length ?? -1,
                result.ModelId,
                result.Items is not null
                    ? string.Join(", ", result.Items.Select(i => $"{i.GetType().Name}({(i is TextContent tc ? tc.Text?.Length : 0)})"))
                    : "(none)");

            responseText = "🤔 Não obtive resposta. Tente reformular sua mensagem.";
        }

        // Adicionar resposta ao histórico real do SK (para function-calling chain)
        history.Add(result);

        return responseText;
    }

    private void LogGeminiHttpFailure(HttpOperationException exception, long chatId, string messageType)
    {
        var requestUri = SanitizeRequestUri(GetExceptionDataValue(exception.Data, "Url"));
        var responseContent = Truncate(exception.ResponseContent);
        var requestPayload = Truncate(GetExceptionDataValue(exception.Data, "Data"));
        var requestMethod = Truncate(GetExceptionDataValue(exception.Data, "Name"));
        var telemetryData = FormatTelemetryData(exception.Data);

        _logger.LogError(
            exception,
            "Falha HTTP ao chamar Gemini. ChatId: {ChatId}. MessageType: {MessageType}. ModelId: {ModelId}. StatusCode: {StatusCode}. RequestMethod: {RequestMethod}. RequestUri: {RequestUri}. RequestPayload: {RequestPayload}. ResponseContent: {ResponseContent}. TelemetryData: {TelemetryData}",
            chatId,
            messageType,
            GeminiModel,
            exception.StatusCode?.ToString() ?? "(not available)",
            requestMethod,
            requestUri,
            requestPayload,
            responseContent,
            telemetryData);
    }

    private static string BuildGeminiFailureMessage(HttpStatusCode? statusCode)
    {
        if (statusCode is HttpStatusCode.ServiceUnavailable or HttpStatusCode.TooManyRequests or HttpStatusCode.GatewayTimeout)
            return "⚠️ O serviço de IA está temporariamente indisponível. Tente novamente em instantes.";

        return "❌ Ocorreu um erro ao processar sua mensagem. Tente novamente em breve.";
    }

    private static string SanitizeRequestUri(string? requestUri)
    {
        if (string.IsNullOrWhiteSpace(requestUri))
            return "(not available)";

        var sanitized = SensitiveQueryStringRegex().Replace(requestUri, "$1***redacted***");
        return Truncate(sanitized);
    }

    private static string Truncate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "(empty)";

        return value.Length <= MaxLoggedPayloadLength
            ? value
            : $"{value[..MaxLoggedPayloadLength]}...(truncated)";
    }

    private static string FormatTelemetryData(System.Collections.IDictionary telemetryData)
    {
        if (telemetryData.Count == 0)
            return "(none)";

        var items = telemetryData
            .Cast<System.Collections.DictionaryEntry>()
            .Select(entry => $"{entry.Key}={Truncate(entry.Value?.ToString())}");

        return Truncate(string.Join("; ", items));
    }

    private static string? GetExceptionDataValue(System.Collections.IDictionary telemetryData, string key)
    {
        return telemetryData.Contains(key)
            ? telemetryData[key]?.ToString()
            : null;
    }

    [GeneratedRegex("([?&](?:key|api_key)=)[^&]+", RegexOptions.IgnoreCase)]
    private static partial Regex SensitiveQueryStringRegex();
}
