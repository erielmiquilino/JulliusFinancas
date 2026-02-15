using System.Text.Json;
using Jullius.ServiceApi.Application.DTOs;

namespace Jullius.ServiceApi.Application.Services;

public class GeminiAssistantService
{
    private readonly BotConfigurationService _configService;
    private readonly ILogger<GeminiAssistantService> _logger;
    private readonly HttpClient _httpClient;

    private const string GeminiModel = "gemini-3-flash-preview";
    private const string GeminiApiBaseUrl = "https://generativelanguage.googleapis.com/v1beta/models";
    private static readonly TimeZoneInfo BrazilTimeZone = GetBrazilTimeZone();

    private static readonly string SystemPrompt = """
        Você é o assistente financeiro do Jullius Finanças. Sua função é classificar a intenção do usuário e extrair dados estruturados da mensagem.

        O usuário pode enviar UMA ou MÚLTIPLAS transações em uma única mensagem.
        Quando houver múltiplas transações (separadas por "e", "E", ";", quebras de linha, ou implícitas na frase), retorne CADA uma como item separado no array "transactions".

        Classifique CADA transação em UMA das intenções:

        1. CREATE_EXPENSE — O usuário RELATA um gasto já realizado (verbos no passado: "gastei", "paguei", "comprei" sem menção a cartão, ou imperativos como "lance", "registre", "anote").
        2. CREATE_CARD_PURCHASE — O usuário relata compra em cartão de crédito (menciona cartão, parcelas, "parcelei", "10x", ou nome de cartão como "nubank", "inter").
        3. FINANCIAL_CONSULTING — O usuário PERGUNTA algo (usa "?", "posso", "como estou", "quanto", "devo", verbos no futuro/condicional).

        REGRAS DE DESAMBIGUAÇÃO:
        - Frases AFIRMATIVAS no passado sem menção a cartão/parcelas = CREATE_EXPENSE
        - Frases AFIRMATIVAS com menção a cartão, parcelas ou nome de cartão = CREATE_CARD_PURCHASE
        - Frases INTERROGATIVAS ou com verbos no futuro/condicional = FINANCIAL_CONSULTING
        - "gastei 50" = CREATE_EXPENSE, "posso gastar 50?" = FINANCIAL_CONSULTING
        - Menção a "débito", "dinheiro", "pix" = CREATE_EXPENSE (não cartão de crédito)

        IMPORTANTE para extração de dados:
        - Extraia valores numéricos de expressões como "45 reais", "R$200", "2000", "2k" (2k = 2000)
        - Para parcelas, extraia de "10x", "em 10 vezes", "em 10 parcelas", "parcelei em 10"
        - Para categorias, extraia texto após "categoria", "em", "na categoria"
        - Para cartões, extraia nomes próprios que pareçam ser cartões (nubank, inter, itaú, etc.)
        - Para vencimento, extraia "dueDate" quando o usuário informar data explícita ou relativa (ex: "amanhã", "próxima segunda", "dia 10/03", "nas próximas 3 segundas"), usando formato ISO 8601 (yyyy-MM-dd)
        - Capitalize a primeira letra da descrição e da categoria
        - Identifique se o usuário indicou que a despesa já foi paga com expressões como: "pago", "paga", "pagas", "pagos", "já paguei", "já pago", "quitado", "quitada". Se sim, isPaid = true. Caso contrário, isPaid = false.
        - Quando o status de pagamento se aplicar a TODAS as transações (ex: "as duas pagas", "todos pagos", "tudo pago"), marque isPaid = true em TODAS.

        Responda SEMPRE e SOMENTE com JSON válido (sem markdown, sem ```):
        {
          "transactions": [
            {
              "intent": "CREATE_EXPENSE | CREATE_CARD_PURCHASE | FINANCIAL_CONSULTING",
              "confidence": 0.0 a 1.0,
              "data": {
                "description": "string ou null",
                "amount": número ou null,
                "categoryName": "string ou null",
                "cardName": "string ou null",
                "installments": número ou null,
                "isPaid": boolean (true se pago, false se não mencionado ou pendente),
                "dueDate": "string ISO 8601 (yyyy-MM-dd) ou null",
                "question": "string ou null"
              },
              "missingFields": ["lista de campos obrigatórios faltantes"],
              "clarificationQuestion": "string ou null — pergunta caso dados estejam ambíguos"
            }
          ]
        }

        Para uma ÚNICA transação, retorne um array com 1 item. Para MÚLTIPLAS, retorne um item por transação.
        """;

    public GeminiAssistantService(
        BotConfigurationService configService,
        ILogger<GeminiAssistantService> logger,
        IHttpClientFactory httpClientFactory)
    {
        _configService = configService;
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient("Gemini");
    }

    public virtual async Task<List<GeminiIntentResponse>?> ClassifyIntentAsync(string userMessage, List<Telegram.ChatMessage>? history = null)
    {
        var apiKey = await _configService.GetDecryptedValueAsync("GeminiApiKey");
        if (string.IsNullOrEmpty(apiKey))
        {
            _logger.LogError("Chave API do Gemini não configurada");
            return null;
        }

        var contents = BuildContents(userMessage, history);
        var requestBody = new
        {
            system_instruction = new { parts = new[] { new { text = $"{SystemPrompt}\n\n{BuildDateContextInstruction(DateTime.UtcNow)}" } } },
            contents,
            generationConfig = new
            {
                temperature = 0.1,
                topP = 0.95,
                maxOutputTokens = 8192,
                responseMimeType = "application/json"
            }
        };

        try
        {
            var url = $"{GeminiApiBaseUrl}/{GeminiModel}:generateContent?key={apiKey}";
            var jsonRequest = JsonSerializer.Serialize(requestBody);
            var response = await _httpClient.PostAsync(url, new StringContent(jsonRequest, System.Text.Encoding.UTF8, "application/json"));

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Erro na API do Gemini. Status: {Status}, Resposta: {Resposta}", response.StatusCode, errorContent);
                return null;
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            return ParseGeminiClassificationResponse(responseContent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao chamar API do Gemini para classificação de intenção");
            return null;
        }
    }

    public virtual async Task<string> GetFinancialAdviceAsync(string question, string financialContext)
    {
        var apiKey = await _configService.GetDecryptedValueAsync("GeminiApiKey");
        if (string.IsNullOrEmpty(apiKey))
            return "❌ Chave API do Gemini não configurada.";

        var advicePrompt = $"""
            Você é um consultor financeiro pessoal do app Jullius Finanças.
            Responda de forma concisa, amigável, com emojis e em português brasileiro.
            Use os dados financeiros abaixo para contextualizar sua resposta.
            Formate valores como R$ X.XXX,XX.
            Não invente dados — use somente o que foi fornecido.

            {financialContext}

            Pergunta do usuário: {question}
            """;

        var requestBody = new
        {
            contents = new[] { new { parts = new[] { new { text = advicePrompt } } } },
            generationConfig = new
            {
                temperature = 0.7,
                topP = 0.9,
                maxOutputTokens = 8192
            }
        };

        try
        {
            var url = $"{GeminiApiBaseUrl}/{GeminiModel}:generateContent?key={apiKey}";
            var jsonRequest = JsonSerializer.Serialize(requestBody);
            var response = await _httpClient.PostAsync(url, new StringContent(jsonRequest, System.Text.Encoding.UTF8, "application/json"));

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Erro na API do Gemini para consultoria. Status: {Status}", response.StatusCode);
                return "❌ Não consegui gerar a análise financeira. Tente novamente.";
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            return ExtractTextFromResponse(responseContent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao gerar consultoria financeira via Gemini");
            return "❌ Erro ao processar sua consulta. Tente novamente.";
        }
    }

    public virtual async Task<GeminiIntentResponse?> ExtractDataFromFollowUpAsync(string userMessage, string contextHint)
    {
        var apiKey = await _configService.GetDecryptedValueAsync("GeminiApiKey");
        if (string.IsNullOrEmpty(apiKey))
            return null;

        var extractionPrompt = $$"""
            O usuário está em uma conversa sobre uma transação financeira.
            {{BuildDateContextInstruction(DateTime.UtcNow)}}
            Contexto: {{contextHint}}
            Mensagem do usuário: "{{userMessage}}"

            Extraia os dados da mensagem e retorne SOMENTE JSON válido (sem markdown):
            {
              "intent": "CONTINUE",
              "confidence": 1.0,
              "data": {
                "description": "string ou null",
                "amount": número ou null,
                "categoryName": "string ou null",
                "cardName": "string ou null",
                "installments": número ou null,
                "isPaid": boolean (true se pago/paga/quitado, false caso contrário),
                "dueDate": "string ISO 8601 (yyyy-MM-dd) ou null",
                "question": null
              },
              "missingFields": [],
              "clarificationQuestion": null
            }

            Se o usuário deu uma resposta curta (ex: "saúde", "nubank", "200"), interprete como resposta ao contexto fornecido.
            Se o usuário mencionar que está pago/paga/quitado, isPaid = true.
            """;

        var requestBody = new
        {
            contents = new[] { new { parts = new[] { new { text = extractionPrompt } } } },
            generationConfig = new
            {
                temperature = 0.1,
                maxOutputTokens = 4096,
                responseMimeType = "application/json"
            }
        };

        try
        {
            var url = $"{GeminiApiBaseUrl}/{GeminiModel}:generateContent?key={apiKey}";
            var jsonRequest = JsonSerializer.Serialize(requestBody);
            var response = await _httpClient.PostAsync(url, new StringContent(jsonRequest, System.Text.Encoding.UTF8, "application/json"));

            if (!response.IsSuccessStatusCode)
                return null;

            var responseContent = await response.Content.ReadAsStringAsync();
            return ParseGeminiResponse(responseContent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao extrair dados de follow-up via Gemini");
            return null;
        }
    }

    private static object[] BuildContents(string userMessage, List<Telegram.ChatMessage>? history)
    {
        var contents = new List<object>();

        if (history != null)
        {
            foreach (var msg in history.TakeLast(6))
            {
                contents.Add(new
                {
                    role = msg.Role == "user" ? "user" : "model",
                    parts = new[] { new { text = msg.Content } }
                });
            }
        }

        contents.Add(new
        {
            role = "user",
            parts = new[] { new { text = userMessage } }
        });

        return contents.ToArray();
    }

    private static string BuildDateContextInstruction(DateTime utcNow)
    {
        var brazilNow = TimeZoneInfo.ConvertTimeFromUtc(utcNow, BrazilTimeZone);
        return $"Data/hora de referência atual (fuso de Brasília - America/Sao_Paulo): {brazilNow:yyyy-MM-dd HH:mm}. Use esse referencial para interpretar datas relativas como \"amanhã\" e \"próxima segunda-feira\".";
    }

    private static TimeZoneInfo GetBrazilTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time");
        }
    }

    private GeminiIntentResponse? ParseGeminiResponse(string responseJson)
    {
        try
        {
            var text = ExtractJsonTextFromGeminiResponse(responseJson);
            if (string.IsNullOrEmpty(text))
                return null;

            return JsonSerializer.Deserialize<GeminiIntentResponse>(text);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao parsear resposta do Gemini: {Response}", responseJson);
            return null;
        }
    }

    /// <summary>
    /// Parseia a resposta do Gemini que retorna { "transactions": [...] }.
    /// </summary>
    private List<GeminiIntentResponse>? ParseGeminiClassificationResponse(string responseJson)
    {
        try
        {
            var text = ExtractJsonTextFromGeminiResponse(responseJson);
            if (string.IsNullOrEmpty(text))
                return null;

            // Tenta parsear como wrapper { "transactions": [...] }
            var result = JsonSerializer.Deserialize<GeminiClassificationResult>(text);
            if (result?.Transactions is { Count: > 0 })
                return result.Transactions;

            // Fallback: tenta parsear como GeminiIntentResponse único (backward compat)
            var single = JsonSerializer.Deserialize<GeminiIntentResponse>(text);
            return single != null ? [single] : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao parsear resposta de classificação do Gemini: {Response}", responseJson);
            return null;
        }
    }

    private string? ExtractJsonTextFromGeminiResponse(string responseJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(responseJson);
            var candidates = doc.RootElement.GetProperty("candidates");
            if (candidates.GetArrayLength() == 0)
            {
                _logger.LogWarning("Resposta do Gemini sem candidatos");
                return null;
            }

            var candidate = candidates[0];

            // Detecta resposta truncada por limite de tokens
            if (candidate.TryGetProperty("finishReason", out var finishReason) &&
                finishReason.GetString() == "MAX_TOKENS")
            {
                _logger.LogWarning("Resposta do Gemini truncada por MAX_TOKENS. Aumente maxOutputTokens ou simplifique a mensagem.");
                return null;
            }

            var text = candidate
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString();

            if (string.IsNullOrEmpty(text))
                return null;

            // Clean potential markdown wrapping
            text = text.Trim();
            if (text.StartsWith("```json")) text = text[7..];
            if (text.StartsWith("```")) text = text[3..];
            if (text.EndsWith("```")) text = text[..^3];
            return text.Trim();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao extrair texto JSON da resposta do Gemini");
            return null;
        }
    }

    private string ExtractTextFromResponse(string responseJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(responseJson);
            return doc.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString() ?? "Sem resposta.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao extrair texto da resposta do Gemini");
            return "❌ Não consegui processar a resposta.";
        }
    }
}
