using Jullius.Domain.Domain.Entities;

namespace Jullius.ServiceApi.Application.DTOs;

public class CardInvoiceResponse
{
    public IEnumerable<CardTransaction> Transactions { get; set; } = new List<CardTransaction>();
    public decimal CurrentLimit { get; set; }
    public decimal InvoiceTotal { get; set; }
    public string CardName { get; set; } = string.Empty;
    public int Month { get; set; }
    public int Year { get; set; }
} 