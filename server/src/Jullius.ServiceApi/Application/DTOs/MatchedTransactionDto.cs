namespace Jullius.ServiceApi.Application.DTOs;

public class MatchedTransactionDto
{
    public Guid CardTransactionId { get; set; }
    public DateTime Date { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string OriginalDescription { get; set; } = string.Empty;
    public string Installment { get; set; } = string.Empty;
}
