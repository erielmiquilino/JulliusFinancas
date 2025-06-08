using Jullius.Domain.Domain.Entities;

namespace Jullius.ServiceApi.Application.DTOs;

public class CreateCardTransactionRequest
{
    public Guid CardId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime Date { get; set; }
    public bool IsInstallment { get; set; } = false;
    public int InstallmentCount { get; set; } = 1;
    public CardTransactionType Type { get; set; } = CardTransactionType.Expense;
} 