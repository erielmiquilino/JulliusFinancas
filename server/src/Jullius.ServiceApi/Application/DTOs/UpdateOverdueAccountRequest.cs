namespace Jullius.ServiceApi.Application.DTOs;

public class UpdateOverdueAccountRequest
{
    public string Description { get; set; } = string.Empty;
    public decimal CurrentDebtValue { get; set; }
}
