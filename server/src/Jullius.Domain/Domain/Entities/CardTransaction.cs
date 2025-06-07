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
    public DateTime CreatedAt { get; private set; }

    // Navigation property
    public Card Card { get; private set; }

    public CardTransaction(Guid cardId, string description, decimal amount, DateTime date, string installment)
    {
        Id = Guid.NewGuid();
        CardId = cardId;
        Description = description;
        Amount = amount;
        Date = date;
        Installment = installment;
        CreatedAt = DateTime.UtcNow;

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
    }

    public void UpdateDetails(string description, decimal amount, DateTime date, string installment)
    {
        Description = description;
        Amount = amount;
        Date = date;
        Installment = installment;

        Validate();
    }
} 