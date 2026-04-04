using Jullius.Domain.Domain.Entities;

namespace Jullius.ServiceApi.Application.Services;

public sealed class CategoryResolutionResult
{
    private CategoryResolutionResult(
        bool isResolved,
        Category? category,
        IReadOnlyList<Category> suggestedCategories,
        string? requestedCategoryName,
        bool shouldSuggestCreatingNewCategory)
    {
        IsResolved = isResolved;
        Category = category;
        SuggestedCategories = suggestedCategories;
        RequestedCategoryName = requestedCategoryName;
        ShouldSuggestCreatingNewCategory = shouldSuggestCreatingNewCategory;
    }

    public bool IsResolved { get; }
    public Category? Category { get; }
    public IReadOnlyList<Category> SuggestedCategories { get; }
    public string? RequestedCategoryName { get; }
    public bool ShouldSuggestCreatingNewCategory { get; }

    public static CategoryResolutionResult Resolved(Category category)
    {
        return new CategoryResolutionResult(true, category, Array.Empty<Category>(), null, false);
    }

    public static CategoryResolutionResult RequiresConfirmation(
        IReadOnlyList<Category> suggestedCategories,
        string? requestedCategoryName,
        bool shouldSuggestCreatingNewCategory)
    {
        return new CategoryResolutionResult(
            false,
            null,
            suggestedCategories,
            requestedCategoryName,
            shouldSuggestCreatingNewCategory);
    }
}
