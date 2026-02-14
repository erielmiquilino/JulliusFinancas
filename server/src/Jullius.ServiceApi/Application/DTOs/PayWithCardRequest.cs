namespace Jullius.ServiceApi.Application.DTOs;

public class PayWithCardRequest
{
    public List<Guid> TransactionIds { get; set; } = new();
    public Guid CardId { get; set; }
    public decimal CardAmount { get; set; }
    public int InvoiceYear { get; set; }
    public int InvoiceMonth { get; set; }
}

