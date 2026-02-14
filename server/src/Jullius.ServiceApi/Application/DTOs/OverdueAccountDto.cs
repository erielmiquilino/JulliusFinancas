namespace Jullius.ServiceApi.Application.DTOs;

public class OverdueAccountDto
{
    public Guid Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal CurrentDebtValue { get; set; }
    public DateTime CreatedAt { get; set; }
}
