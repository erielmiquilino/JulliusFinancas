using Jullius.Domain.Domain.Entities;
using Jullius.Domain.Domain.Repositories;
using Jullius.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace Jullius.Data.Repositories;

public class BotConfigurationRepository(JulliusDbContext context) : IBotConfigurationRepository
{
    public async Task<BotConfiguration> CreateAsync(BotConfiguration config)
    {
        await context.Set<BotConfiguration>().AddAsync(config);
        await context.SaveChangesAsync();
        return config;
    }

    public async Task<BotConfiguration?> GetByKeyAsync(string configKey)
    {
        return await context.Set<BotConfiguration>()
            .FirstOrDefaultAsync(c => c.ConfigKey == configKey);
    }

    public async Task<IEnumerable<BotConfiguration>> GetAllAsync()
    {
        return await context.Set<BotConfiguration>()
            .OrderBy(c => c.ConfigKey)
            .ToListAsync();
    }

    public async Task UpdateAsync(BotConfiguration config)
    {
        context.Set<BotConfiguration>().Update(config);
        await context.SaveChangesAsync();
    }

    public async Task DeleteAsync(string configKey)
    {
        var config = await GetByKeyAsync(configKey);
        if (config != null)
        {
            context.Set<BotConfiguration>().Remove(config);
            await context.SaveChangesAsync();
        }
    }
}
