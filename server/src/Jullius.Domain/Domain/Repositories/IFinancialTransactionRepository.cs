using Jullius.Domain.Domain.Entities;

namespace Jullius.Domain.Domain.Repositories;

public interface IFinancialTransactionRepository
{
    Task<FinancialTransaction> CreateAsync(FinancialTransaction transaction);
    Task<FinancialTransaction?> GetByIdAsync(Guid id);
    Task<IEnumerable<FinancialTransaction>> GetAllAsync();
    Task<FinancialTransaction?> GetByCardIdAndPeriodAsync(Guid cardId, int year, int month);
    Task<IEnumerable<FinancialTransaction>> GetByCardIdAsync(Guid cardId);
    Task UpdateAsync(FinancialTransaction transaction);
    Task DeleteAsync(Guid id);
} 