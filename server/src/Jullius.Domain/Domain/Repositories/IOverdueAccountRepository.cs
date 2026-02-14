using Jullius.Domain.Domain.Entities;

namespace Jullius.Domain.Domain.Repositories;

public interface IOverdueAccountRepository
{
    Task<OverdueAccount> CreateAsync(OverdueAccount overdueAccount);
    Task<OverdueAccount?> GetByIdAsync(Guid id);
    Task<IEnumerable<OverdueAccount>> GetAllAsync();
    Task UpdateAsync(OverdueAccount overdueAccount);
    Task DeleteAsync(Guid id);
}
