using Jullius.Domain.Domain.Entities;

namespace Jullius.Domain.Domain.Repositories;

public interface ICategoryRepository
{
    Task<Category> CreateAsync(Category category);
    Task<Category?> GetByIdAsync(Guid id);
    Task<Category?> GetByNameAsync(string name);
    Task<IEnumerable<Category>> GetAllAsync();
    Task UpdateAsync(Category category);
    Task DeleteAsync(Guid id);
    Task<bool> IsInUseAsync(Guid id);
    Task<Category> GetOrCreateSystemCategoryAsync(string name, string color);
}

