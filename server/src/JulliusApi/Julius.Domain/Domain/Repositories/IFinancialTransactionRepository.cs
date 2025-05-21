using Julius.Domain.Domain.Entities;

namespace Julius.Domain.Domain.Repositories;

public interface IFinancialTransactionRepository
{
    Task<FinancialTransaction> CreateAsync(FinancialTransaction transaction);
    Task<FinancialTransaction?> GetByIdAsync(Guid id);
    Task<IEnumerable<FinancialTransaction>> GetAllAsync();
    Task UpdateAsync(FinancialTransaction transaction);
    Task DeleteAsync(Guid id);
} 