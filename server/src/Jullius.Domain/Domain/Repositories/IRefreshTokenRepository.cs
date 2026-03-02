using Jullius.Domain.Domain.Entities;

namespace Jullius.Domain.Domain.Repositories;

public interface IRefreshTokenRepository
{
    Task<RefreshToken> CreateAsync(RefreshToken refreshToken);
    Task<RefreshToken?> GetByTokenAsync(string token);
    Task<IEnumerable<RefreshToken>> GetActiveByUserIdAsync(Guid userId);
    Task UpdateAsync(RefreshToken refreshToken);
    Task RevokeAllByUserIdAsync(Guid userId);
}
