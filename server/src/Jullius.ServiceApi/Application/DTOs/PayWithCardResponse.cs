namespace Jullius.ServiceApi.Application.DTOs;

public class PayWithCardResponse
{
    public int PaidTransactionsCount { get; set; }
    public Guid IncomeTransactionId { get; set; }
    public List<Guid> CardTransactionIds { get; set; } = new();
}

