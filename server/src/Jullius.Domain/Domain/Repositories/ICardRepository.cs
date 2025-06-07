using Jullius.Domain.Domain.Entities;

namespace Jullius.Domain.Domain.Repositories;

public interface ICardRepository
{
    Task<Card> CreateAsync(Card card);
    Task<Card?> GetByIdAsync(Guid id);
    Task<IEnumerable<Card>> GetAllAsync();
    Task UpdateAsync(Card card);
    Task DeleteAsync(Guid id);
} 