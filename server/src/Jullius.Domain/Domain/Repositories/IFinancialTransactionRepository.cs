using Jullius.Domain.Domain.Entities;

namespace Jullius.Domain.Domain.Repositories;

public interface IFinancialTransactionRepository
{
    Task<FinancialTransaction> CreateAsync(FinancialTransaction transaction);
    Task<FinancialTransaction?> GetByIdAsync(Guid id);
    Task<IEnumerable<FinancialTransaction>> GetAllAsync();
    Task<FinancialTransaction?> GetByDescriptionAndPeriodAsync(string description, int year, int month);
    Task<IEnumerable<FinancialTransaction>> GetByDescriptionAsync(string description);
    Task UpdateAsync(FinancialTransaction transaction);
    Task DeleteAsync(Guid id);
} 