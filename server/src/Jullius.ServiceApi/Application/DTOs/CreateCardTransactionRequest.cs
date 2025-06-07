namespace Jullius.ServiceApi.Application.DTOs;

public class CreateCardTransactionRequest
{
    public Guid CardId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime Date { get; set; }
    public string Installment { get; set; } = string.Empty;
} 