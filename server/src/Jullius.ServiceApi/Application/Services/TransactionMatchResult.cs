using Jullius.Domain.Domain.Entities;

namespace Jullius.ServiceApi.Application.Services;

public sealed class TransactionMatchResult
{
    private TransactionMatchResult(TransactionMatchStatus status, IReadOnlyList<FinancialTransaction> matches)
    {
        Status = status;
        Matches = matches;
    }

    public TransactionMatchStatus Status { get; }
    public IReadOnlyList<FinancialTransaction> Matches { get; }

    public FinancialTransaction? SingleMatch => Status == TransactionMatchStatus.Unique ? Matches[0] : null;

    public static TransactionMatchResult NotFound()
    {
        return new TransactionMatchResult(TransactionMatchStatus.NotFound, Array.Empty<FinancialTransaction>());
    }

    public static TransactionMatchResult Unique(FinancialTransaction transaction)
    {
        return new TransactionMatchResult(TransactionMatchStatus.Unique, new[] { transaction });
    }

    public static TransactionMatchResult Ambiguous(IReadOnlyList<FinancialTransaction> matches)
    {
        return new TransactionMatchResult(TransactionMatchStatus.Ambiguous, matches);
    }
}

public enum TransactionMatchStatus
{
    NotFound,
    Unique,
    Ambiguous
}
