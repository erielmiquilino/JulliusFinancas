using Jullius.Domain.Domain.Entities;
using Jullius.Domain.Domain.Repositories;
using Jullius.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace Jullius.Data.Repositories;

public class CardTransactionRepository(JulliusDbContext context) : ICardTransactionRepository
{
    public async Task<CardTransaction> CreateAsync(CardTransaction cardTransaction)
    {
        await context.Set<CardTransaction>().AddAsync(cardTransaction);
        await context.SaveChangesAsync();
        return cardTransaction;
    }

    public async Task DeleteAsync(Guid id)
    {
        var cardTransaction = await GetByIdAsync(id);
        if (cardTransaction != null)
        {
            context.Set<CardTransaction>().Remove(cardTransaction);
            await context.SaveChangesAsync();
        }
    }

    public async Task<IEnumerable<CardTransaction>> GetAllAsync()
    {
        return await context.Set<CardTransaction>()
            .Include(ct => ct.Card)
            .ToListAsync();
    }

    public async Task<IEnumerable<CardTransaction>> GetByCardIdAsync(Guid cardId)
    {
        return await context.Set<CardTransaction>()
            .Where(ct => ct.CardId == cardId)
            .Include(ct => ct.Card)
            .OrderByDescending(ct => ct.Date)
            .ToListAsync();
    }

    public async Task<IEnumerable<CardTransaction>> GetByCardIdAndPeriodAsync(Guid cardId, int month, int year)
    {
        return await context.Set<CardTransaction>()
            .Where(ct => ct.CardId == cardId && 
                        ct.InvoiceMonth == month && 
                        ct.InvoiceYear == year)
            .Include(ct => ct.Card)
            .OrderByDescending(ct => ct.Date)
            .ToListAsync();
    }

    public async Task<IEnumerable<CardTransaction>> GetByCardIdFromPeriodAsync(Guid cardId, int month, int year)
    {
        return await context.Set<CardTransaction>()
            .Where(ct => ct.CardId == cardId && 
                        (ct.InvoiceYear > year || 
                         (ct.InvoiceYear == year && ct.InvoiceMonth >= month)))
            .Include(ct => ct.Card)
            .OrderByDescending(ct => ct.Date)
            .ToListAsync();
    }

    public async Task<CardTransaction?> GetByIdAsync(Guid id)
    {
        return await context.Set<CardTransaction>()
            .Include(ct => ct.Card)
            .FirstOrDefaultAsync(ct => ct.Id == id);
    }

    public async Task<IEnumerable<CardTransaction>> GetByCardIdAndDateRangeAsync(Guid cardId, DateTime startDate, DateTime endDate, bool excludeInstallments = true)
    {
        var query = context.Set<CardTransaction>()
            .Where(ct => ct.CardId == cardId && 
                        ct.Date >= startDate && 
                        ct.Date <= endDate);

        if (excludeInstallments)
        {
            // Exclui transações parceladas (que não sejam "1/1")
            query = query.Where(ct => ct.Installment == "1/1");
        }

        return await query
            .Include(ct => ct.Card)
            .OrderByDescending(ct => ct.Date)
            .ToListAsync();
    }

    public async Task UpdateAsync(CardTransaction cardTransaction)
    {
        context.Set<CardTransaction>().Update(cardTransaction);
        await context.SaveChangesAsync();
    }

    public async Task<IEnumerable<string>> GetDistinctDescriptionsAsync(string searchTerm)
    {
        return await context.Set<CardTransaction>()
            .Where(ct => ct.Description.Contains(searchTerm))
            .Select(ct => ct.Description)
            .Distinct()
            .OrderBy(d => d)
            .Take(20)
            .ToListAsync();
    }
} 