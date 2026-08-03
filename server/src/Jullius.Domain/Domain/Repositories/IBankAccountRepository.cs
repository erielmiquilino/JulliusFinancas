using Jullius.Domain.Domain.Entities;

namespace Jullius.Domain.Domain.Repositories;

public interface IBankAccountRepository
{
    Task<BankAccount> CreateAsync(BankAccount bankAccount);
    Task<BankAccount?> GetByIdAsync(Guid id);
    Task<BankAccount?> GetByPluggyAccountIdAsync(string pluggyAccountId);
    Task<IEnumerable<BankAccount>> GetAllAsync();
    Task<IEnumerable<BankAccount>> GetActiveAsync();
    Task UpdateAsync(BankAccount bankAccount);
    Task DeleteAsync(Guid id);
}
