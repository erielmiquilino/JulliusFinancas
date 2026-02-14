using Jullius.Domain.Domain.Entities;

namespace Jullius.ServiceApi.Application.DTOs;

public class UpdateFinancialTransactionRequest
{
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime DueDate { get; set; }
    public TransactionType Type { get; set; }
    public Guid CategoryId { get; set; }
    public bool IsPaid { get; set; }
    public Guid? BudgetId { get; set; }
} 