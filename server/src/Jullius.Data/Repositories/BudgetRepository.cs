using Jullius.Domain.Domain.Entities;
using Jullius.Domain.Domain.Repositories;
using Jullius.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace Jullius.Data.Repositories;

public class BudgetRepository(JulliusDbContext context) : IBudgetRepository
{
    public async Task<Budget> CreateAsync(Budget budget)
    {
        await context.Set<Budget>().AddAsync(budget);
        await context.SaveChangesAsync();
        return budget;
    }

    public async Task DeleteAsync(Guid id)
    {
        var budget = await GetByIdAsync(id);
        if (budget != null)
        {
            context.Set<Budget>().Remove(budget);
            await context.SaveChangesAsync();
        }
    }

    public async Task<IEnumerable<Budget>> GetAllAsync()
    {
        return await context.Set<Budget>()
            .OrderByDescending(b => b.Year)
            .ThenByDescending(b => b.Month)
            .ThenBy(b => b.Name)
            .ToListAsync();
    }

    public async Task<IEnumerable<Budget>> GetByMonthAndYearAsync(int month, int year)
    {
        return await context.Set<Budget>()
            .Where(b => b.Month == month && b.Year == year)
            .OrderBy(b => b.Name)
            .ToListAsync();
    }

    public async Task<Budget?> GetByIdAsync(Guid id)
    {
        return await context.Set<Budget>().FindAsync(id);
    }

    public async Task<bool> IsInUseAsync(Guid id)
    {
        return await context.Set<FinancialTransaction>()
            .AnyAsync(ft => ft.BudgetId == id);
    }

    public async Task<decimal> GetUsedAmountAsync(Guid budgetId)
    {
        return await context.Set<FinancialTransaction>()
            .Where(ft => ft.BudgetId == budgetId && ft.IsPaid && ft.Type == TransactionType.PayableBill)
            .SumAsync(ft => ft.Amount);
    }

    public async Task UpdateAsync(Budget budget)
    {
        context.Set<Budget>().Update(budget);
        await context.SaveChangesAsync();
    }
}

