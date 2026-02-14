namespace Jullius.ServiceApi.Application.DTOs;

public class UnmatchedTransactionDto
{
    public DateTime Date { get; set; }
    public string OriginalDescription { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string MappedDescription { get; set; } = string.Empty;
    public bool IsInstallment { get; set; }
}
