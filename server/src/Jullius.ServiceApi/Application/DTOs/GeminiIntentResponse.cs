using System.Text.Json.Serialization;

namespace Jullius.ServiceApi.Application.DTOs;

/// <summary>
/// Wrapper que o Gemini retorna — sempre contém um array de transações.
/// </summary>
public class GeminiClassificationResult
{
    [JsonPropertyName("transactions")]
    public List<GeminiIntentResponse> Transactions { get; set; } = [];
}

public class GeminiIntentResponse
{
    [JsonPropertyName("intent")]
    public string Intent { get; set; } = string.Empty;

    [JsonPropertyName("confidence")]
    public double Confidence { get; set; }

    [JsonPropertyName("data")]
    public GeminiExtractedData Data { get; set; } = new();

    [JsonPropertyName("missingFields")]
    public List<string> MissingFields { get; set; } = [];

    [JsonPropertyName("clarificationQuestion")]
    public string? ClarificationQuestion { get; set; }
}

public class GeminiExtractedData
{
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("amount")]
    public decimal? Amount { get; set; }

    [JsonPropertyName("categoryName")]
    public string? CategoryName { get; set; }

    [JsonPropertyName("cardName")]
    public string? CardName { get; set; }

    [JsonPropertyName("installments")]
    public int? Installments { get; set; }

    [JsonPropertyName("isPaid")]
    public bool? IsPaid { get; set; }

    [JsonPropertyName("question")]
    public string? Question { get; set; }
}
