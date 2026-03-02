using Jullius.Domain.Domain.Entities;

namespace Jullius.Domain.Domain.Repositories;

public interface IUserRepository
{
    Task<User> CreateAsync(User user);
    Task<User?> GetByIdAsync(Guid id);
    Task<User?> GetByEmailAsync(string email);
    Task<IEnumerable<User>> GetAllAsync();
    Task UpdateAsync(User user);
    Task<bool> ExistsByEmailAsync(string email);
}
