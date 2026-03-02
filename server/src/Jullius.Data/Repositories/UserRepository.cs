using Jullius.Domain.Domain.Entities;
using Jullius.Domain.Domain.Repositories;
using Jullius.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace Jullius.Data.Repositories;

public class UserRepository(JulliusDbContext context) : IUserRepository
{
    public async Task<User> CreateAsync(User user)
    {
        await context.Set<User>().AddAsync(user);
        await context.SaveChangesAsync();
        return user;
    }

    public async Task<User?> GetByIdAsync(Guid id)
    {
        return await context.Set<User>().FindAsync(id);
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        return await context.Set<User>()
            .FirstOrDefaultAsync(u => u.Email == email.ToLower());
    }

    public async Task<IEnumerable<User>> GetAllAsync()
    {
        return await context.Set<User>()
            .OrderBy(u => u.Name)
            .ToListAsync();
    }

    public async Task UpdateAsync(User user)
    {
        context.Set<User>().Update(user);
        await context.SaveChangesAsync();
    }

    public async Task<bool> ExistsByEmailAsync(string email)
    {
        return await context.Set<User>()
            .AnyAsync(u => u.Email == email.ToLower());
    }
}
