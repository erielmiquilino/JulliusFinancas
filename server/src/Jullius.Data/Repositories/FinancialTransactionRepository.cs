using Jullius.Domain.Domain.Entities;
using Jullius.Domain.Domain.Repositories;
using Jullius.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace Jullius.Data.Repositories;

public class FinancialTransactionRepository(JulliusDbContext context) : IFinancialTransactionRepository
{
    public async Task<FinancialTransaction> CreateAsync(FinancialTransaction transaction)
    {
        await context.Set<FinancialTransaction>().AddAsync(transaction);
        await context.SaveChangesAsync();
        return transaction;
    }

    public async Task DeleteAsync(Guid id)
    {
        var transaction = await GetByIdAsync(id);
        context.Set<FinancialTransaction>().Remove(transaction!);
        await context.SaveChangesAsync();
    }

    public async Task DeleteManyAsync(IEnumerable<Guid> ids)
    {
        var transactions = await context.Set<FinancialTransaction>()
            .Where(ft => ids.Contains(ft.Id))
            .ToListAsync();
        context.Set<FinancialTransaction>().RemoveRange(transactions);
        await context.SaveChangesAsync();
    }

    public async Task<IEnumerable<FinancialTransaction>> GetAllAsync()
    {
        return await context.Set<FinancialTransaction>()
            .Include(ft => ft.Category)
            .ToListAsync();
    }

    public async Task<FinancialTransaction?> GetByIdAsync(Guid id)
    {
        return await context.Set<FinancialTransaction>()
            .Include(ft => ft.Category)
            .FirstOrDefaultAsync(ft => ft.Id == id);
    }


    public async Task<FinancialTransaction?> GetByCardIdAndPeriodAsync(Guid cardId, int year, int month)
    {
        return await context.Set<FinancialTransaction>()
            .FirstOrDefaultAsync(ft => ft.CardId == cardId && 
                                      ft.DueDate.Year == year && 
                                      ft.DueDate.Month == month);
    }

    public async Task<IEnumerable<FinancialTransaction>> GetByCardIdAsync(Guid cardId)
    {
        return await context.Set<FinancialTransaction>()
            .Where(ft => ft.CardId == cardId)
            .ToListAsync();
    }

    public async Task UpdateAsync(FinancialTransaction transaction)
    {
        context.Set<FinancialTransaction>().Update(transaction);
        await context.SaveChangesAsync();
    }

    public async Task<IEnumerable<string>> GetDistinctDescriptionsAsync(string searchTerm)
    {
        return await context.Set<FinancialTransaction>()
            .Where(ft => ft.Description.Contains(searchTerm))
            .Select(ft => ft.Description)
            .Distinct()
            .OrderBy(d => d)
            .Take(20)
            .ToListAsync();
    }
} 