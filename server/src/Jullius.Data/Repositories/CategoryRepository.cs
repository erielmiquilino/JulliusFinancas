using Jullius.Domain.Domain.Entities;
using Jullius.Domain.Domain.Repositories;
using Jullius.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace Jullius.Data.Repositories;

public class CategoryRepository(JulliusDbContext context) : ICategoryRepository
{
    public async Task<Category> CreateAsync(Category category)
    {
        await context.Set<Category>().AddAsync(category);
        await context.SaveChangesAsync();
        return category;
    }

    public async Task DeleteAsync(Guid id)
    {
        var category = await GetByIdAsync(id);
        if (category != null)
        {
            context.Set<Category>().Remove(category);
            await context.SaveChangesAsync();
        }
    }

    public async Task<IEnumerable<Category>> GetAllAsync()
    {
        return await context.Set<Category>()
            .OrderBy(c => c.Name)
            .ToListAsync();
    }

    public async Task<Category?> GetByIdAsync(Guid id)
    {
        return await context.Set<Category>().FindAsync(id);
    }

    public async Task<Category?> GetByNameAsync(string name)
    {
        return await context.Set<Category>()
            .FirstOrDefaultAsync(c => c.Name == name);
    }

    public async Task<bool> IsInUseAsync(Guid id)
    {
        return await context.Set<FinancialTransaction>()
            .AnyAsync(ft => ft.CategoryId == id);
    }

    public async Task UpdateAsync(Category category)
    {
        context.Set<Category>().Update(category);
        await context.SaveChangesAsync();
    }

    public async Task<Category> GetOrCreateSystemCategoryAsync(string name, string color)
    {
        var existingCategory = await GetByNameAsync(name);
        if (existingCategory != null)
            return existingCategory;

        var newCategory = new Category(name, color);
        return await CreateAsync(newCategory);
    }
}

