using Jullius.Data.Context;
using Jullius.Domain.Domain.Entities;
using Jullius.Domain.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Jullius.Data.Repositories;

public class ReconciliationRepository(JulliusDbContext context) : IReconciliationRepository
{
    public async Task<ReconciliationSession> CreateSessionAsync(ReconciliationSession session)
    {
        await context.Set<ReconciliationSession>().AddAsync(session);
        await context.SaveChangesAsync();
        return session;
    }

    public async Task<ReconciliationSession?> GetSessionByIdAsync(Guid sessionId)
    {
        return await context.Set<ReconciliationSession>()
            .FirstOrDefaultAsync(x => x.Id == sessionId);
    }

    public async Task<ReconciliationSession?> GetOpenSessionAsync()
    {
        return await context.Set<ReconciliationSession>()
            .Where(x => x.Status == ReconciliationSessionStatus.Draft)
            .OrderByDescending(x => x.StartedAt)
            .FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<ReconciliationSession>> GetSessionsAsync(int limit)
    {
        return await context.Set<ReconciliationSession>()
            .OrderByDescending(x => x.StartedAt)
            .Take(limit)
            .ToListAsync();
    }

    public async Task UpdateSessionAsync(ReconciliationSession session)
    {
        context.Set<ReconciliationSession>().Update(session);
        await context.SaveChangesAsync();
    }

    public async Task AddItemsAsync(IEnumerable<ReconciliationItem> items)
    {
        await context.Set<ReconciliationItem>().AddRangeAsync(items);
        await context.SaveChangesAsync();
    }

    public async Task<ReconciliationItem?> GetItemByIdAsync(Guid itemId)
    {
        return await context.Set<ReconciliationItem>()
            .Include(x => x.BankAccount)
            .FirstOrDefaultAsync(x => x.Id == itemId);
    }

    public async Task<IEnumerable<ReconciliationItem>> GetItemsBySessionAsync(Guid sessionId)
    {
        return await context.Set<ReconciliationItem>()
            .Include(x => x.BankAccount)
            .Where(x => x.SessionId == sessionId)
            .OrderBy(x => x.RawDate)
            .ThenBy(x => x.RawDescription)
            .ToListAsync();
    }

    public async Task<IReadOnlyCollection<string>> GetKnownExternalIdsAsync(IEnumerable<string> externalIds)
    {
        // Precisa ser List, não array: sobre string[] o compilador liga o Contains à sobrecarga
        // de MemoryExtensions baseada em ReadOnlySpan<T>, e a árvore de expressão do EF não
        // consegue compilar um ref struct como argumento genérico.
        var candidates = externalIds.Distinct().ToList();
        if (candidates.Count == 0)
            return Array.Empty<string>();

        return await context.Set<ReconciliationItem>()
            .Where(x => candidates.Contains(x.ExternalId))
            .Select(x => x.ExternalId)
            .ToListAsync();
    }

    public async Task<IEnumerable<ReconciliationItem>> GetIgnoredItemsAsync()
    {
        return await context.Set<ReconciliationItem>()
            .Include(x => x.BankAccount)
            .Where(x => x.Status == ReconciliationItemStatus.Ignored)
            .OrderByDescending(x => x.RawDate)
            .ToListAsync();
    }

    public async Task UpdateItemAsync(ReconciliationItem item)
    {
        context.Set<ReconciliationItem>().Update(item);
        await context.SaveChangesAsync();
    }

    public async Task UpdateItemsAsync(IEnumerable<ReconciliationItem> items)
    {
        context.Set<ReconciliationItem>().UpdateRange(items);
        await context.SaveChangesAsync();
    }

    public async Task DeleteItemsBySessionAsync(Guid sessionId)
    {
        var items = await context.Set<ReconciliationItem>()
            .Where(x => x.SessionId == sessionId)
            .ToListAsync();

        context.Set<ReconciliationItem>().RemoveRange(items);
        await context.SaveChangesAsync();
    }
}
