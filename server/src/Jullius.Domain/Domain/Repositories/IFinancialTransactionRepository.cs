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
    Task DeleteManyAsync(IEnumerable<Guid> ids);
    Task<IEnumerable<string>> GetDistinctDescriptionsAsync(string searchTerm);

    /// <summary>Transações com vencimento dentro do intervalo (limites inclusivos).</summary>
    Task<IEnumerable<FinancialTransaction>> GetByDueDateRangeAsync(DateTime from, DateTime to);

    /// <summary>
    /// Soma do realizado (IsPaid) no intervalo: receitas recebidas menos despesas pagas.
    /// Base do saldo consolidado do dashboard.
    /// </summary>
    Task<decimal> GetRealizedNetAmountAsync(DateTime from, DateTime to);
} 