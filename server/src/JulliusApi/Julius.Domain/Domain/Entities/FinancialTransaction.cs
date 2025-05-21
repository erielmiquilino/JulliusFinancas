using System.ComponentModel.DataAnnotations;

namespace Julius.Domain.Domain.Entities;

public class FinancialTransaction
{
    [Key]
    public Guid Id { get; private set; }
    public string Description { get; private set; }
    public decimal Amount { get; private set; }
    public DateTime DueDate { get; private set; }
    public TransactionType Type { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public FinancialTransaction(string description, decimal amount, DateTime dueDate, TransactionType type)
    {
        Id = Guid.NewGuid();
        Description = description;
        Amount = amount;
        DueDate = dueDate;
        Type = type;
        CreatedAt = DateTime.UtcNow;

        Validate();
    }

    private void Validate()
    {
        if (string.IsNullOrWhiteSpace(Description))
            throw new ArgumentException("Description cannot be empty");

        if (Amount <= 0)
            throw new ArgumentException("Amount must be greater than zero");
    }

    public void UpdateDetails(string description, decimal amount, DateTime dueDate, TransactionType type)
    {
        Description = description;
        Amount = amount;
        DueDate = dueDate;
        Type = type;
    }
}

public enum TransactionType
{
    PayableBill,
    ReceivableBill
} 