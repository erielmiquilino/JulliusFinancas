using System.ComponentModel.DataAnnotations;

namespace Jullius.Domain.Domain.Entities;

public class Budget
{
    [Key]
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public decimal LimitAmount { get; private set; }
    public string? Description { get; private set; }
    public int Month { get; private set; }
    public int Year { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public Budget(string name, decimal limitAmount, int month, int year, string? description = null)
    {
        Id = Guid.NewGuid();
        Name = name;
        LimitAmount = limitAmount;
        Month = month;
        Year = year;
        Description = description;
        CreatedAt = DateTime.UtcNow;

        Validate();
    }

    // For Entity Framework
    private Budget() { }

    private void Validate()
    {
        if (string.IsNullOrWhiteSpace(Name))
            throw new ArgumentException("Name cannot be empty");

        if (LimitAmount <= 0)
            throw new ArgumentException("Limit amount must be greater than zero");

        if (Month < 1 || Month > 12)
            throw new ArgumentException("Month must be between 1 and 12");

        if (Year < 2000 || Year > 2100)
            throw new ArgumentException("Year must be between 2000 and 2100");
    }

    public void Update(string name, decimal limitAmount, int month, int year, string? description = null)
    {
        Name = name;
        LimitAmount = limitAmount;
        Month = month;
        Year = year;
        Description = description;

        Validate();
    }
}

