using Jullius.Domain.Domain.Entities;

namespace Jullius.Domain.Domain.Repositories;

public interface IPasswordResetTokenRepository
{
    Task<PasswordResetToken> CreateAsync(PasswordResetToken token);
    Task<PasswordResetToken?> GetByTokenAsync(string token);
    Task UpdateAsync(PasswordResetToken token);
    Task InvalidateAllByUserIdAsync(Guid userId);
}
