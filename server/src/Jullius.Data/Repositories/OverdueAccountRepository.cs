using Jullius.Domain.Domain.Entities;
using Jullius.Domain.Domain.Repositories;
using Jullius.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace Jullius.Data.Repositories;

public class OverdueAccountRepository(JulliusDbContext context) : IOverdueAccountRepository
{
    public async Task<OverdueAccount> CreateAsync(OverdueAccount overdueAccount)
    {
        await context.Set<OverdueAccount>().AddAsync(overdueAccount);
        await context.SaveChangesAsync();
        return overdueAccount;
    }

    public async Task DeleteAsync(Guid id)
    {
        var overdueAccount = await GetByIdAsync(id);
        if (overdueAccount != null)
        {
            context.Set<OverdueAccount>().Remove(overdueAccount);
            await context.SaveChangesAsync();
        }
    }

    public async Task<IEnumerable<OverdueAccount>> GetAllAsync()
    {
        return await context.Set<OverdueAccount>()
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();
    }

    public async Task<OverdueAccount?> GetByIdAsync(Guid id)
    {
        return await context.Set<OverdueAccount>().FindAsync(id);
    }

    public async Task UpdateAsync(OverdueAccount overdueAccount)
    {
        context.Set<OverdueAccount>().Update(overdueAccount);
        await context.SaveChangesAsync();
    }
}
