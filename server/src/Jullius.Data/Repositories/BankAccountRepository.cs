using Jullius.Data.Context;
using Jullius.Domain.Domain.Entities;
using Jullius.Domain.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Jullius.Data.Repositories;

public class BankAccountRepository(JulliusDbContext context) : IBankAccountRepository
{
    public async Task<BankAccount> CreateAsync(BankAccount bankAccount)
    {
        await context.Set<BankAccount>().AddAsync(bankAccount);
        await context.SaveChangesAsync();
        return bankAccount;
    }

    public async Task<BankAccount?> GetByIdAsync(Guid id)
    {
        return await context.Set<BankAccount>()
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<BankAccount?> GetByPluggyAccountIdAsync(string pluggyAccountId)
    {
        return await context.Set<BankAccount>()
            .FirstOrDefaultAsync(x => x.PluggyAccountId == pluggyAccountId);
    }

    public async Task<IEnumerable<BankAccount>> GetAllAsync()
    {
        return await context.Set<BankAccount>()
            .OrderBy(x => x.Institution)
            .ThenBy(x => x.Name)
            .ToListAsync();
    }

    public async Task<IEnumerable<BankAccount>> GetActiveAsync()
    {
        return await context.Set<BankAccount>()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Institution)
            .ThenBy(x => x.Name)
            .ToListAsync();
    }

    public async Task UpdateAsync(BankAccount bankAccount)
    {
        context.Set<BankAccount>().Update(bankAccount);
        await context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var bankAccount = await GetByIdAsync(id);
        if (bankAccount == null)
            return;

        context.Set<BankAccount>().Remove(bankAccount);
        await context.SaveChangesAsync();
    }
}
