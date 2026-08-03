using System.Text.Json.Serialization;

namespace Jullius.ServiceApi.Integrations.Pluggy;

public sealed class PluggyAuthResponse
{
    [JsonPropertyName("apiKey")]
    public string? ApiKey { get; set; }
}

public sealed class PluggyItem
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("executionStatus")]
    public string? ExecutionStatus { get; set; }

    [JsonPropertyName("lastUpdatedAt")]
    public DateTime? LastUpdatedAt { get; set; }

    [JsonPropertyName("connector")]
    public PluggyConnector? Connector { get; set; }
}

public sealed class PluggyConnector
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }
}

public sealed class PluggyAccountList
{
    [JsonPropertyName("results")]
    public List<PluggyAccount> Results { get; set; } = new();
}

public sealed class PluggyAccount
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("subtype")]
    public string? Subtype { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("number")]
    public string? Number { get; set; }

    [JsonPropertyName("balance")]
    public decimal Balance { get; set; }

    [JsonPropertyName("owner")]
    public string? Owner { get; set; }

    [JsonPropertyName("itemId")]
    public string? ItemId { get; set; }

    /// <summary>CREDIT indica cartão de crédito, que está fora do escopo desta versão.</summary>
    public bool IsCreditCard =>
        string.Equals(Type, "CREDIT", StringComparison.OrdinalIgnoreCase);
}

public sealed class PluggyTransactionPage
{
    [JsonPropertyName("results")]
    public List<PluggyTransaction> Results { get; set; } = new();

    /// <summary>Cursor da próxima página. Pode vir relativo ("?accountId=...") ou absoluto.</summary>
    [JsonPropertyName("next")]
    public string? Next { get; set; }
}

public sealed class PluggyTransaction
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("descriptionRaw")]
    public string? DescriptionRaw { get; set; }

    /// <summary>Em conta corrente, negativo é saída.</summary>
    [JsonPropertyName("amount")]
    public decimal Amount { get; set; }

    [JsonPropertyName("date")]
    public DateTime Date { get; set; }

    [JsonPropertyName("category")]
    public string? Category { get; set; }

    [JsonPropertyName("categoryId")]
    public string? CategoryId { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("accountId")]
    public string? AccountId { get; set; }

    [JsonPropertyName("paymentData")]
    public PluggyPaymentData? PaymentData { get; set; }

    public bool IsPosted => string.Equals(Status, "POSTED", StringComparison.OrdinalIgnoreCase);
}

public sealed class PluggyPaymentData
{
    [JsonPropertyName("paymentMethod")]
    public string? PaymentMethod { get; set; }

    [JsonPropertyName("payer")]
    public PluggyPaymentParty? Payer { get; set; }

    [JsonPropertyName("receiver")]
    public PluggyPaymentParty? Receiver { get; set; }
}

public sealed class PluggyPaymentParty
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("documentNumber")]
    public PluggyDocumentNumber? DocumentNumber { get; set; }
}

public sealed class PluggyDocumentNumber
{
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("value")]
    public string? Value { get; set; }
}

/// <summary>
/// Sinaliza que o item da Pluggy não existe mais (HTTP 404), o que acontece quando a conexão
/// é excluída e recriada no Meu Pluggy — o itemId muda. As contas do item morto continuam
/// respondendo 200 com dados congelados, por isso o item é sempre validado antes do sync.
/// </summary>
public sealed class PluggyItemNotFoundException(string itemId)
    : Exception($"O item {itemId} não existe mais na Pluggy. Reconecte a conta e atualize o itemId cadastrado.")
{
    public string ItemId { get; } = itemId;
}
