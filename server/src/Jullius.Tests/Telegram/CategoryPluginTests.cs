using FluentAssertions;
using Jullius.Domain.Domain.Entities;
using Jullius.Domain.Domain.Repositories;
using Jullius.ServiceApi.Application.Services;
using Jullius.ServiceApi.Telegram.Plugins;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Jullius.Tests.Telegram;

/// <summary>
/// Testes para o CategoryPlugin (ListCategories, CreateCategory, DeleteCategory).
/// </summary>
public class CategoryPluginTests
{
    private readonly Mock<ICategoryRepository> _categoryRepoMock = new();
    private readonly CategoryPlugin _plugin;

    public CategoryPluginTests()
    {
        var categoryService = new CategoryService(_categoryRepoMock.Object);
        _plugin = new CategoryPlugin(
            categoryService,
            Mock.Of<ILogger<CategoryPlugin>>());
    }

    [Fact]
    public async Task ListCategories_ShouldReturnAllCategories()
    {
        var categories = new List<Category>
        {
            new("Alimentação", "#FF5722"),
            new("Transporte", "#2196F3"),
            new("Saúde", "#4CAF50")
        };
        _categoryRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(categories);

        var result = await _plugin.ListCategoriesAsync();

        Assert.Contains("Alimentação", result);
        Assert.Contains("Transporte", result);
        Assert.Contains("Saúde", result);
    }

    [Fact]
    public async Task ListCategories_ShouldReturnEmptyMessage_WhenNoCategories()
    {
        _categoryRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(Array.Empty<Category>());

        var result = await _plugin.ListCategoriesAsync();

        Assert.Contains("Nenhuma categoria", result);
    }

    [Fact]
    public async Task CreateCategory_ShouldReturnConfirmation()
    {
        _categoryRepoMock
            .Setup(r => r.CreateAsync(It.IsAny<Category>()))
            .ReturnsAsync((Category c) => c);

        var result = await _plugin.CreateCategoryAsync("Lazer", "#9C27B0");

        Assert.Contains("✅", result);
        Assert.Contains("Lazer", result);
    }

    [Fact]
    public async Task DeleteCategory_ShouldReturnSuccess_WhenCategoryDeleted()
    {
        var category = new Category("Lazer", "#9C27B0");
        _categoryRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new[] { category });
        _categoryRepoMock.Setup(r => r.GetByIdAsync(category.Id)).ReturnsAsync(category);
        _categoryRepoMock.Setup(r => r.IsInUseAsync(category.Id)).ReturnsAsync(false);
        _categoryRepoMock.Setup(r => r.DeleteAsync(category.Id)).Returns(Task.CompletedTask);

        var result = await _plugin.DeleteCategoryAsync("Lazer");

        Assert.Contains("✅", result);
        Assert.Contains("removida", result.ToLowerInvariant());
    }

    [Fact]
    public async Task DeleteCategory_ShouldReturnError_WhenCategoryNotFound()
    {
        _categoryRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(Array.Empty<Category>());

        var result = await _plugin.DeleteCategoryAsync("Inexistente");

        Assert.Contains("❌", result);
        Assert.Contains("não encontrada", result.ToLowerInvariant());
    }

    [Fact]
    public async Task DeleteCategory_ShouldReturnError_WhenCategoryInUse()
    {
        var category = new Category("Moradia", "#FF5722");
        _categoryRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new[] { category });
        _categoryRepoMock.Setup(r => r.GetByIdAsync(category.Id)).ReturnsAsync(category);
        _categoryRepoMock.Setup(r => r.IsInUseAsync(category.Id)).ReturnsAsync(true);

        var result = await _plugin.DeleteCategoryAsync("Moradia");

        Assert.Contains("❌", result);
        Assert.Contains("em uso", result.ToLowerInvariant());
    }
}
