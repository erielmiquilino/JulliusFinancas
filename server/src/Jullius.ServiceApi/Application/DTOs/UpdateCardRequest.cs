namespace Jullius.ServiceApi.Application.DTOs;

public class UpdateCardRequest
{
    public string Name { get; set; } = string.Empty;
    public string IssuingBank { get; set; } = string.Empty;
    public int ClosingDay { get; set; }
    public int DueDay { get; set; }
    public decimal Limit { get; set; }
} 