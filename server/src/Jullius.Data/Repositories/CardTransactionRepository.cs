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
                        ct.Date.Month == month && 
                        ct.Date.Year == year)
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

    public async Task UpdateAsync(CardTransaction cardTransaction)
    {
        context.Set<CardTransaction>().Update(cardTransaction);
        await context.SaveChangesAsync();
    }
} 