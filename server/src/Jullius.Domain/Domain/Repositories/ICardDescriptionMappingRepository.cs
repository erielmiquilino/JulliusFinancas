using Jullius.Domain.Domain.Entities;

namespace Jullius.Domain.Domain.Repositories;

public interface ICardDescriptionMappingRepository
{
    Task<CardDescriptionMapping> CreateAsync(CardDescriptionMapping mapping);
    Task<CardDescriptionMapping?> GetByIdAsync(Guid id);
    Task<CardDescriptionMapping?> GetByOriginalDescriptionAndCardAsync(string originalDescription, Guid cardId);
    Task<IEnumerable<CardDescriptionMapping>> GetByCardIdAsync(Guid cardId);
    Task UpdateAsync(CardDescriptionMapping mapping);
    Task DeleteAsync(Guid id);
}
