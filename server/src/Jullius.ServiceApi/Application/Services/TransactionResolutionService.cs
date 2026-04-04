using Jullius.Domain.Domain.Entities;
using Jullius.Domain.Domain.Repositories;

namespace Jullius.ServiceApi.Application.Services;

public sealed class TransactionResolutionService
{
    private const int MaxAmbiguousMatches = 5;
    private const decimal MinimumSimilarity = 0.35m;

    private readonly IFinancialTransactionRepository _transactionRepository;

    public TransactionResolutionService(IFinancialTransactionRepository transactionRepository)
    {
        _transactionRepository = transactionRepository;
    }

    public async Task<TransactionMatchResult> ResolveAsync(
        string searchDescription,
        decimal? amount = null,
        DateTime? dueDate = null)
    {
        var transactions = await _transactionRepository.GetAllAsync();

        var matches = transactions
            .Select(transaction => new TransactionCandidate(transaction, CalculateScore(transaction, searchDescription, amount, dueDate)))
            .Where(candidate => candidate.Score > 0m)
            .OrderByDescending(candidate => candidate.Score)
            .ThenByDescending(candidate => candidate.Transaction.CreatedAt)
            .Select(candidate => candidate.Transaction)
            .ToArray();

        if (matches.Length == 0)
            return TransactionMatchResult.NotFound();

        if (matches.Length == 1)
            return TransactionMatchResult.Unique(matches[0]);

        return TransactionMatchResult.Ambiguous(matches.Take(MaxAmbiguousMatches).ToArray());
    }

    private static decimal CalculateScore(
        FinancialTransaction transaction,
        string searchDescription,
        decimal? amount,
        DateTime? dueDate)
    {
        var descriptionScore = TextSearchNormalizer.CalculateSimilarity(searchDescription, transaction.Description);
        if (descriptionScore < MinimumSimilarity)
            return 0m;

        if (amount.HasValue && transaction.Amount != amount.Value)
            return 0m;

        if (dueDate.HasValue && transaction.DueDate.Date != dueDate.Value.Date)
            return 0m;

        var score = descriptionScore;

        if (amount.HasValue)
            score += 0.2m;

        if (dueDate.HasValue)
            score += 0.2m;

        return score;
    }

    private sealed record TransactionCandidate(FinancialTransaction Transaction, decimal Score);
}
