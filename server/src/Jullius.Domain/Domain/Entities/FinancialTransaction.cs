using System.ComponentModel.DataAnnotations;

namespace Jullius.Domain.Domain.Entities;

public class FinancialTransaction
{
    [Key]
    public Guid Id { get; private set; }
    public string Description { get; private set; }
    public decimal Amount { get; private set; }
    public DateTime DueDate { get; private set; }
    public TransactionType Type { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public bool IsPaid { get; private set; }
    public Guid? CardId { get; private set; } // Relacionamento opcional com cartão
    public Guid CategoryId { get; private set; } // Relacionamento obrigatório com categoria
    public Guid? BudgetId { get; private set; } // Relacionamento opcional com budget

    // Navigation properties
    public Category Category { get; private set; }
    public Budget? Budget { get; private set; }

    public FinancialTransaction(string description, decimal amount, DateTime dueDate, TransactionType type, Guid categoryId, bool isPaid = false, Guid? cardId = null, Guid? budgetId = null)
    {
        Id = Guid.NewGuid();
        Description = description;
        Amount = amount;
        DueDate = dueDate;
        Type = type;
        CategoryId = categoryId;
        CreatedAt = DateTime.UtcNow;
        IsPaid = isPaid;
        CardId = cardId;
        BudgetId = budgetId;

        Validate();
    }

    // For Entity Framework
    private FinancialTransaction() { }

    private void Validate()
    {
        if (string.IsNullOrWhiteSpace(Description))
            throw new ArgumentException("Description cannot be empty");

        if (Amount <= 0)
            throw new ArgumentException("Amount must be greater than zero");

        if (CategoryId == Guid.Empty)
            throw new ArgumentException("CategoryId cannot be empty");
    }

    public void UpdateDetails(string description, decimal amount, DateTime dueDate, TransactionType type, Guid categoryId, bool isPaid, Guid? cardId = null, Guid? budgetId = null)
    {
        Description = description;
        Amount = amount;
        DueDate = dueDate;
        Type = type;
        CategoryId = categoryId;
        IsPaid = isPaid;
        CardId = cardId;
        BudgetId = budgetId;
    }

    public void UpdatePaymentStatus(bool isPaid)
    {
        IsPaid = isPaid;
    }
}

public enum TransactionType
{
    PayableBill,
    ReceivableBill
} 