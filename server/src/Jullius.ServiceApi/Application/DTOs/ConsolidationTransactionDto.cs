using Jullius.Domain.Domain.Entities;

namespace Jullius.ServiceApi.Application.DTOs;

public class ConsolidationTransactionDto
{
    public DateTime Date { get; set; }
    public string OriginalDescription { get; set; } = string.Empty;
    public string MappedDescription { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public CardTransactionType Type { get; set; } = CardTransactionType.Expense;
    public int InvoiceYear { get; set; }
    public int InvoiceMonth { get; set; }
}
