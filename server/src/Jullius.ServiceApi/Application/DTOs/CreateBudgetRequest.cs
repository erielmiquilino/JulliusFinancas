namespace Jullius.ServiceApi.Application.DTOs;

public class CreateBudgetRequest
{
    public string Name { get; set; } = string.Empty;
    public decimal LimitAmount { get; set; }
    public string? Description { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }
}

