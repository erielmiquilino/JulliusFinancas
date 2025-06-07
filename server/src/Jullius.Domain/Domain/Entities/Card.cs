using System.ComponentModel.DataAnnotations;

namespace Jullius.Domain.Domain.Entities;

public class Card
{
    [Key]
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public string IssuingBank { get; private set; }
    public int ClosingDay { get; private set; }
    public int DueDay { get; private set; }
    public decimal Limit { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public Card(string name, string issuingBank, int closingDay, int dueDay, decimal limit)
    {
        Id = Guid.NewGuid();
        Name = name;
        IssuingBank = issuingBank;
        ClosingDay = closingDay;
        DueDay = dueDay;
        Limit = limit;
        CreatedAt = DateTime.UtcNow;

        Validate();
    }

    // For Entity Framework
    private Card() { }

    private void Validate()
    {
        if (string.IsNullOrWhiteSpace(Name))
            throw new ArgumentException("Name cannot be empty");

        if (string.IsNullOrWhiteSpace(IssuingBank))
            throw new ArgumentException("IssuingBank cannot be empty");

        if (ClosingDay < 1 || ClosingDay > 31)
            throw new ArgumentException("ClosingDay must be between 1 and 31");

        if (DueDay < 1 || DueDay > 31)
            throw new ArgumentException("DueDay must be between 1 and 31");

        if (Limit <= 0)
            throw new ArgumentException("Limit must be greater than zero");
    }

    public void UpdateDetails(string name, string issuingBank, int closingDay, int dueDay, decimal limit)
    {
        Name = name;
        IssuingBank = issuingBank;
        ClosingDay = closingDay;
        DueDay = dueDay;
        Limit = limit;

        Validate();
    }
} 