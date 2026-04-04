using Jullius.Domain.Domain.Entities;
using Jullius.Domain.Domain.Repositories;

namespace Jullius.ServiceApi.Application.Services;

public sealed class CategoryResolutionService
{
    private const decimal AutoResolveThreshold = 0.75m;
    private const decimal AutoResolveGapThreshold = 0.12m;
    private const int MaxSuggestedCategories = 5;

    private readonly ICategoryRepository _categoryRepository;
    private readonly IFinancialTransactionRepository _transactionRepository;

    public CategoryResolutionService(
        ICategoryRepository categoryRepository,
        IFinancialTransactionRepository transactionRepository)
    {
        _categoryRepository = categoryRepository;
        _transactionRepository = transactionRepository;
    }

    public async Task<CategoryResolutionResult> ResolveAsync(string description, string? requestedCategoryName)
    {
        var categories = (await _categoryRepository.GetAllAsync()).ToList();
        if (categories.Count == 0)
        {
            return CategoryResolutionResult.RequiresConfirmation(
                Array.Empty<Category>(),
                SanitizeCategoryName(requestedCategoryName),
                !string.IsNullOrWhiteSpace(requestedCategoryName));
        }

        var sanitizedCategoryName = SanitizeCategoryName(requestedCategoryName);
        if (!string.IsNullOrWhiteSpace(sanitizedCategoryName))
            return ResolveRequestedCategory(categories, sanitizedCategoryName);

        return await ResolveCategoryFromHistoryAsync(categories, description);
    }

    private CategoryResolutionResult ResolveRequestedCategory(IReadOnlyList<Category> categories, string requestedCategoryName)
    {
        var exactMatch = FindExactMatch(categories, requestedCategoryName);
        if (exactMatch != null)
            return CategoryResolutionResult.Resolved(exactMatch);

        var partialMatches = FindPartialMatches(categories, requestedCategoryName);
        if (partialMatches.Count == 1)
            return CategoryResolutionResult.Resolved(partialMatches[0]);

        var suggestions = partialMatches.Count > 0
            ? partialMatches.Take(MaxSuggestedCategories).ToArray()
            : categories.Take(MaxSuggestedCategories).ToArray();

        return CategoryResolutionResult.RequiresConfirmation(
            suggestions,
            requestedCategoryName,
            true);
    }

    private async Task<CategoryResolutionResult> ResolveCategoryFromHistoryAsync(
        IReadOnlyList<Category> categories,
        string description)
    {
        var suggestions = await GetHistorySuggestionsAsync(description, categories);

        if (CanAutoResolve(suggestions))
            return CategoryResolutionResult.Resolved(suggestions[0].Category);

        var suggestedCategories = suggestions.Count > 0
            ? suggestions.Select(x => x.Category).Take(MaxSuggestedCategories).ToArray()
            : categories.Take(MaxSuggestedCategories).ToArray();

        return CategoryResolutionResult.RequiresConfirmation(
            suggestedCategories,
            null,
            false);
    }

    private async Task<IReadOnlyList<CategoryScore>> GetHistorySuggestionsAsync(
        string description,
        IReadOnlyList<Category> categories)
    {
        var transactions = await _transactionRepository.GetAllAsync();

        var scores = transactions
            .Select(transaction => CreateCategoryScoreCandidate(description, categories, transaction))
            .Where(candidate => candidate != null)
            .Select(candidate => candidate!)
            .GroupBy(candidate => candidate.Category.Id)
            .Select(group => new CategoryScore(
                group.First().Category,
                group.Max(item => item.Score),
                group.Average(item => item.Score),
                group.Count()))
            .OrderByDescending(score => score.FinalScore)
            .ThenBy(score => score.Category.Name)
            .ToArray();

        return scores;
    }

    private static CategoryScoreCandidate? CreateCategoryScoreCandidate(
        string description,
        IReadOnlyList<Category> categories,
        FinancialTransaction transaction)
    {
        var score = TextSearchNormalizer.CalculateSimilarity(description, transaction.Description);
        if (score <= 0m)
            return null;

        var category = transaction.Category ?? categories.FirstOrDefault(item => item.Id == transaction.CategoryId);
        if (category == null)
            return null;

        return new CategoryScoreCandidate(category, score);
    }

    private static bool CanAutoResolve(IReadOnlyList<CategoryScore> suggestions)
    {
        if (suggestions.Count == 0)
            return false;

        var best = suggestions[0];
        var second = suggestions.Count > 1 ? suggestions[1] : null;
        var gap = second == null ? decimal.MaxValue : best.FinalScore - second.FinalScore;

        return best.FinalScore >= AutoResolveThreshold && gap >= AutoResolveGapThreshold;
    }

    private static Category? FindExactMatch(IEnumerable<Category> categories, string requestedCategoryName)
    {
        var normalizedRequestedName = TextSearchNormalizer.Normalize(requestedCategoryName);

        return categories.FirstOrDefault(category =>
            TextSearchNormalizer.Normalize(category.Name).Equals(normalizedRequestedName, StringComparison.Ordinal));
    }

    private static IReadOnlyList<Category> FindPartialMatches(IEnumerable<Category> categories, string requestedCategoryName)
    {
        var normalizedRequestedName = TextSearchNormalizer.Normalize(requestedCategoryName);

        return categories
            .Where(category => IsPartialMatch(category.Name, normalizedRequestedName))
            .OrderBy(category => category.Name)
            .ToArray();
    }

    private static bool IsPartialMatch(string categoryName, string normalizedRequestedName)
    {
        var normalizedCategoryName = TextSearchNormalizer.Normalize(categoryName);

        return normalizedCategoryName.Contains(normalizedRequestedName, StringComparison.Ordinal) ||
               normalizedRequestedName.Contains(normalizedCategoryName, StringComparison.Ordinal);
    }

    private static string? SanitizeCategoryName(string? categoryName)
    {
        return string.IsNullOrWhiteSpace(categoryName)
            ? null
            : categoryName.Trim();
    }

    private sealed record CategoryScoreCandidate(Category Category, decimal Score);

    private sealed record CategoryScore(Category Category, decimal BestScore, decimal AverageScore, int MatchCount)
    {
        public decimal FinalScore => BestScore + Math.Min(MatchCount, 3) * 0.03m + AverageScore * 0.05m;
    }
}
