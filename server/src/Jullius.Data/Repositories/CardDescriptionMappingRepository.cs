using Jullius.Domain.Domain.Entities;
using Jullius.Domain.Domain.Repositories;
using Jullius.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace Jullius.Data.Repositories;

public class CardDescriptionMappingRepository(JulliusDbContext context) : ICardDescriptionMappingRepository
{
    public async Task<CardDescriptionMapping> CreateAsync(CardDescriptionMapping mapping)
    {
        await context.Set<CardDescriptionMapping>().AddAsync(mapping);
        await context.SaveChangesAsync();
        return mapping;
    }

    public async Task<CardDescriptionMapping?> GetByIdAsync(Guid id)
    {
        return await context.Set<CardDescriptionMapping>()
            .Include(cdm => cdm.Card)
            .FirstOrDefaultAsync(cdm => cdm.Id == id);
    }

    public async Task<CardDescriptionMapping?> GetByOriginalDescriptionAndCardAsync(string originalDescription, Guid cardId)
    {
        return await context.Set<CardDescriptionMapping>()
            .Include(cdm => cdm.Card)
            .FirstOrDefaultAsync(cdm => cdm.OriginalDescription == originalDescription && cdm.CardId == cardId);
    }

    public async Task<IEnumerable<CardDescriptionMapping>> GetByCardIdAsync(Guid cardId)
    {
        return await context.Set<CardDescriptionMapping>()
            .Where(cdm => cdm.CardId == cardId)
            .Include(cdm => cdm.Card)
            .OrderBy(cdm => cdm.OriginalDescription)
            .ToListAsync();
    }

    public async Task UpdateAsync(CardDescriptionMapping mapping)
    {
        context.Set<CardDescriptionMapping>().Update(mapping);
        await context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var mapping = await GetByIdAsync(id);
        if (mapping != null)
        {
            context.Set<CardDescriptionMapping>().Remove(mapping);
            await context.SaveChangesAsync();
        }
    }
}
