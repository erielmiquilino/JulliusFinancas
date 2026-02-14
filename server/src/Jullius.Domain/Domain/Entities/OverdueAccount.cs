using System.ComponentModel.DataAnnotations;

namespace Jullius.Domain.Domain.Entities;

public class OverdueAccount
{
    [Key]
    public Guid Id { get; private set; }
    public string Description { get; private set; }
    public decimal CurrentDebtValue { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public OverdueAccount(string description, decimal currentDebtValue)
    {
        Id = Guid.NewGuid();
        Description = description;
        CurrentDebtValue = currentDebtValue;
        CreatedAt = DateTime.UtcNow;

        Validate();
    }

    // For Entity Framework
    private OverdueAccount() { }

    private void Validate()
    {
        if (string.IsNullOrWhiteSpace(Description))
            throw new ArgumentException("Description cannot be empty");

        if (CurrentDebtValue < 0)
            throw new ArgumentException("Current debt value cannot be negative");
    }

    public void Update(string description, decimal currentDebtValue)
    {
        Description = description;
        CurrentDebtValue = currentDebtValue;
        Validate();
    }
}
