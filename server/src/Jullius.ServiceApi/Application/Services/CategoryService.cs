using Jullius.Domain.Domain.Entities;
using Jullius.Domain.Domain.Repositories;
using Jullius.ServiceApi.Application.DTOs;

namespace Jullius.ServiceApi.Application.Services;

public class CategoryService
{
    private readonly ICategoryRepository _repository;

    public CategoryService(ICategoryRepository repository)
    {
        _repository = repository;
    }

    public async Task<CategoryDto> CreateCategoryAsync(CreateCategoryRequest request)
    {
        var category = new Category(request.Name, request.Color);
        var created = await _repository.CreateAsync(category);
        return MapToDto(created);
    }

    public async Task<IEnumerable<CategoryDto>> GetAllCategoriesAsync()
    {
        var categories = await _repository.GetAllAsync();
        return categories.Select(MapToDto);
    }

    public async Task<CategoryDto?> GetCategoryByIdAsync(Guid id)
    {
        var category = await _repository.GetByIdAsync(id);
        return category == null ? null : MapToDto(category);
    }

    public async Task<CategoryDto?> UpdateCategoryAsync(Guid id, UpdateCategoryRequest request)
    {
        var category = await _repository.GetByIdAsync(id);
        if (category == null)
            return null;

        category.Update(request.Name, request.Color);
        await _repository.UpdateAsync(category);
        return MapToDto(category);
    }

    public async Task<(bool Success, string? ErrorMessage)> DeleteCategoryAsync(Guid id)
    {
        var category = await _repository.GetByIdAsync(id);
        if (category == null)
            return (false, "Categoria não encontrada");

        var isInUse = await _repository.IsInUseAsync(id);
        if (isInUse)
            return (false, "Não é possível excluir uma categoria que está em uso");

        await _repository.DeleteAsync(id);
        return (true, null);
    }

    private static CategoryDto MapToDto(Category category)
    {
        return new CategoryDto
        {
            Id = category.Id,
            Name = category.Name,
            Color = category.Color,
            CreatedAt = category.CreatedAt
        };
    }
}

