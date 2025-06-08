using Jullius.Domain.Domain.Entities;

namespace Jullius.ServiceApi.Application.DTOs;

public class UpdateCardTransactionRequest
{
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime Date { get; set; }
    public string Installment { get; set; } = string.Empty;
    public CardTransactionType Type { get; set; } = CardTransactionType.Expense;
} 