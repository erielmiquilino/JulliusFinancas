using Jullius.Domain.Domain.Entities;
using Jullius.Domain.Domain.Repositories;
using Jullius.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace Jullius.Data.Repositories;

public class RefreshTokenRepository(JulliusDbContext context) : IRefreshTokenRepository
{
    public async Task<RefreshToken> CreateAsync(RefreshToken refreshToken)
    {
        await context.Set<RefreshToken>().AddAsync(refreshToken);
        await context.SaveChangesAsync();
        return refreshToken;
    }

    public async Task<RefreshToken?> GetByTokenAsync(string token)
    {
        return await context.Set<RefreshToken>()
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.Token == token);
    }

    public async Task<IEnumerable<RefreshToken>> GetActiveByUserIdAsync(Guid userId)
    {
        return await context.Set<RefreshToken>()
            .Where(rt => rt.UserId == userId && !rt.IsRevoked && rt.ExpiresAt > DateTime.UtcNow)
            .ToListAsync();
    }

    public async Task UpdateAsync(RefreshToken refreshToken)
    {
        context.Set<RefreshToken>().Update(refreshToken);
        await context.SaveChangesAsync();
    }

    public async Task RevokeAllByUserIdAsync(Guid userId)
    {
        var activeTokens = await context.Set<RefreshToken>()
            .Where(rt => rt.UserId == userId && !rt.IsRevoked)
            .ToListAsync();

        foreach (var token in activeTokens)
        {
            token.Revoke();
        }

        await context.SaveChangesAsync();
    }
}
