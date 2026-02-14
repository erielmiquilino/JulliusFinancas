namespace Jullius.ServiceApi.Application.DTOs;

public class BudgetDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal LimitAmount { get; set; }
    public string? Description { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }
    public DateTime CreatedAt { get; set; }
    public decimal UsedAmount { get; set; }
    public decimal RemainingAmount => LimitAmount - UsedAmount;
    public decimal UsagePercentage => LimitAmount > 0 ? (UsedAmount / LimitAmount) * 100 : 0;
}

