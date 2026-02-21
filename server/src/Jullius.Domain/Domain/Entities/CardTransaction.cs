using System.ComponentModel.DataAnnotations;

namespace Jullius.Domain.Domain.Entities;

public class CardTransaction
{
    [Key]
    public Guid Id { get; private set; }
    public Guid CardId { get; private set; }
    public string Description { get; private set; }
    public decimal Amount { get; private set; }
    public DateTime Date { get; private set; }
    public string Installment { get; private set; } // Ex: "1/1", "2/12", etc.
    public int InvoiceYear { get; private set; } // Ano da fatura a qual pertence
    public int InvoiceMonth { get; private set; } // Mês da fatura a qual pertence
    public DateTime CreatedAt { get; private set; }
    public CardTransactionType Type { get; private set; }

    // Navigation property
    public Card Card { get; private set; }

    public CardTransaction(Guid cardId, string description, decimal amount, DateTime date, string installment, int invoiceYear, int invoiceMonth, CardTransactionType type = CardTransactionType.Expense)
    {
        Id = Guid.NewGuid();
        CardId = cardId;
        Description = description;
        Amount = amount;
        Date = EnsureUtc(date);
        Installment = installment;
        InvoiceYear = invoiceYear;
        InvoiceMonth = invoiceMonth;
        CreatedAt = DateTime.UtcNow;
        Type = type;

        Validate();
    }

    // For Entity Framework
    private CardTransaction() { }

    private void Validate()
    {
        if (CardId == Guid.Empty)
            throw new ArgumentException("CardId cannot be empty");

        if (string.IsNullOrWhiteSpace(Description))
            throw new ArgumentException("Description cannot be empty");

        if (Amount <= 0)
            throw new ArgumentException("Amount must be greater than zero");

        if (string.IsNullOrWhiteSpace(Installment))
            throw new ArgumentException("Installment cannot be empty");

        if (InvoiceYear < 1900 || InvoiceYear > 2100)
            throw new ArgumentException("Invoice year must be valid");

        if (InvoiceMonth < 1 || InvoiceMonth > 12)
            throw new ArgumentException("Invoice month must be between 1 and 12");
    }

    public void UpdateDetails(string description, decimal amount, DateTime date, string installment, int invoiceYear, int invoiceMonth, CardTransactionType type)
    {
        Description = description;
        Amount = amount;
        Date = EnsureUtc(date);
        Installment = installment;
        InvoiceYear = invoiceYear;
        InvoiceMonth = invoiceMonth;
        Type = type;

        Validate();
    }
    private static DateTime EnsureUtc(DateTime dateTime) =>
        dateTime.Kind == DateTimeKind.Utc ? dateTime : DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);}

public enum CardTransactionType
{
    Expense = 0,
    Income = 1
} 