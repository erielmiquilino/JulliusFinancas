namespace Jullius.ServiceApi.Application.DTOs;

public class CreateOverdueAccountRequest
{
    public string Description { get; set; } = string.Empty;
    public decimal CurrentDebtValue { get; set; }
}
