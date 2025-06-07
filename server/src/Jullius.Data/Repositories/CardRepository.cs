using Jullius.Domain.Domain.Entities;
using Jullius.Domain.Domain.Repositories;
using Jullius.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace Jullius.Data.Repositories;

public class CardRepository(JulliusDbContext context) : ICardRepository
{
    public async Task<Card> CreateAsync(Card card)
    {
        await context.Set<Card>().AddAsync(card);
        await context.SaveChangesAsync();
        return card;
    }

    public async Task DeleteAsync(Guid id)
    {
        var card = await GetByIdAsync(id);
        context.Set<Card>().Remove(card!);
        await context.SaveChangesAsync();
    }

    public async Task<IEnumerable<Card>> GetAllAsync()
    {
        return await context.Set<Card>().ToListAsync();
    }

    public async Task<Card?> GetByIdAsync(Guid id)
    {
        return await context.Set<Card>().FindAsync(id);
    }

    public async Task UpdateAsync(Card card)
    {
        context.Set<Card>().Update(card);
        await context.SaveChangesAsync();
    }
} 