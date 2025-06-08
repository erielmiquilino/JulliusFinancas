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

    public async Task<IEnumerable<FinancialTransaction>> GetAllAsync()
    {
        return await context.Set<FinancialTransaction>().ToListAsync();
    }

    public async Task<FinancialTransaction?> GetByIdAsync(Guid id)
    {
        return await context.Set<FinancialTransaction>().FindAsync(id);
    }

    public async Task<FinancialTransaction?> GetByDescriptionAndPeriodAsync(string description, int year, int month)
    {
        return await context.Set<FinancialTransaction>()
            .FirstOrDefaultAsync(ft => ft.Description == description && 
                                      ft.DueDate.Year == year && 
                                      ft.DueDate.Month == month);
    }

    public async Task UpdateAsync(FinancialTransaction transaction)
    {
        context.Set<FinancialTransaction>().Update(transaction);
        await context.SaveChangesAsync();
    }
} 