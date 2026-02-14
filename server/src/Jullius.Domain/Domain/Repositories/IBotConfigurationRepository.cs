using Jullius.Domain.Domain.Entities;

namespace Jullius.Domain.Domain.Repositories;

public interface IBotConfigurationRepository
{
    Task<BotConfiguration> CreateAsync(BotConfiguration config);
    Task<BotConfiguration?> GetByKeyAsync(string configKey);
    Task<IEnumerable<BotConfiguration>> GetAllAsync();
    Task UpdateAsync(BotConfiguration config);
    Task DeleteAsync(string configKey);
}
