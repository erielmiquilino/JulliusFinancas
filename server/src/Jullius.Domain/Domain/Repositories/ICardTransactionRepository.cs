using Jullius.Domain.Domain.Entities;

namespace Jullius.Domain.Domain.Repositories;

public interface ICardTransactionRepository
{
    Task<CardTransaction> CreateAsync(CardTransaction cardTransaction);
    Task<CardTransaction?> GetByIdAsync(Guid id);
    Task<IEnumerable<CardTransaction>> GetAllAsync();
    Task<IEnumerable<CardTransaction>> GetByCardIdAsync(Guid cardId);
    Task<IEnumerable<CardTransaction>> GetByCardIdAndPeriodAsync(Guid cardId, int month, int year);
    Task<IEnumerable<CardTransaction>> GetByCardIdFromPeriodAsync(Guid cardId, int month, int year);
    Task<IEnumerable<CardTransaction>> GetByCardIdAndDateRangeAsync(Guid cardId, DateTime startDate, DateTime endDate, bool excludeInstallments = true);
    Task UpdateAsync(CardTransaction cardTransaction);
    Task DeleteAsync(Guid id);
    Task<IEnumerable<string>> GetDistinctDescriptionsAsync(string searchTerm);
} 