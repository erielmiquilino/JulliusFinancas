using Jullius.Domain.Domain.Entities;

namespace Jullius.Domain.Domain.Repositories;

public interface IReconciliationRepository
{
    Task<ReconciliationSession> CreateSessionAsync(ReconciliationSession session);
    Task<ReconciliationSession?> GetSessionByIdAsync(Guid sessionId);
    Task<ReconciliationSession?> GetOpenSessionAsync();
    Task<IEnumerable<ReconciliationSession>> GetSessionsAsync(int limit);
    Task UpdateSessionAsync(ReconciliationSession session);

    Task AddItemsAsync(IEnumerable<ReconciliationItem> items);
    Task<ReconciliationItem?> GetItemByIdAsync(Guid itemId);
    Task<IEnumerable<ReconciliationItem>> GetItemsBySessionAsync(Guid sessionId);

    /// <summary>
    /// Chave de idempotência: devolve os ExternalIds já conhecidos entre os candidatos informados,
    /// independentemente do status (inclusive ignorados, que nunca devem reaparecer).
    /// </summary>
    Task<IReadOnlyCollection<string>> GetKnownExternalIdsAsync(IEnumerable<string> externalIds);

    Task<IEnumerable<ReconciliationItem>> GetIgnoredItemsAsync();
    Task UpdateItemAsync(ReconciliationItem item);
    Task UpdateItemsAsync(IEnumerable<ReconciliationItem> items);
    Task DeleteItemsBySessionAsync(Guid sessionId);
}
