using Jullius.Domain.Domain.Entities;
using Jullius.Domain.Domain.Repositories;
using Jullius.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace Jullius.Data.Repositories;

public class PasswordResetTokenRepository(JulliusDbContext context) : IPasswordResetTokenRepository
{
    public async Task<PasswordResetToken> CreateAsync(PasswordResetToken token)
    {
        await context.Set<PasswordResetToken>().AddAsync(token);
        await context.SaveChangesAsync();
        return token;
    }

    public async Task<PasswordResetToken?> GetByTokenAsync(string token)
    {
        return await context.Set<PasswordResetToken>()
            .Include(prt => prt.User)
            .FirstOrDefaultAsync(prt => prt.Token == token);
    }

    public async Task UpdateAsync(PasswordResetToken token)
    {
        context.Set<PasswordResetToken>().Update(token);
        await context.SaveChangesAsync();
    }

    public async Task InvalidateAllByUserIdAsync(Guid userId)
    {
        var activeTokens = await context.Set<PasswordResetToken>()
            .Where(prt => prt.UserId == userId && !prt.IsUsed && prt.ExpiresAt > DateTime.UtcNow)
            .ToListAsync();

        foreach (var token in activeTokens)
        {
            token.MarkAsUsed();
        }

        await context.SaveChangesAsync();
    }
}
