using Jullius.Domain.Domain.Entities;

namespace Jullius.Domain.Domain.Repositories;

public interface IBudgetRepository
{
    Task<Budget> CreateAsync(Budget budget);
    Task<Budget?> GetByIdAsync(Guid id);
    Task<IEnumerable<Budget>> GetAllAsync();
    Task<IEnumerable<Budget>> GetByMonthAndYearAsync(int month, int year);
    Task UpdateAsync(Budget budget);
    Task DeleteAsync(Guid id);
    Task<bool> IsInUseAsync(Guid id);
    Task<decimal> GetUsedAmountAsync(Guid budgetId);
}

