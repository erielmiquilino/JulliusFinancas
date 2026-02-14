using FluentAssertions;
using Jullius.Domain.Domain.Entities;
using Jullius.ServiceApi.Application.DTOs;
using Jullius.ServiceApi.Application.Services;
using Jullius.Tests.Mocks;
using Moq;
using Xunit;

namespace Jullius.Tests.Services;

public class CategoryServiceTests
{
    private readonly RepositoryMocks _mocks;
    private readonly CategoryService _service;

    public CategoryServiceTests()
    {
        _mocks = new RepositoryMocks();
        _service = new CategoryService(_mocks.CategoryRepository.Object);
    }

    #region CreateCategoryAsync Tests

    [Fact]
    public async Task CreateCategoryAsync_WithValidData_ShouldCreateCategory()
    {
        // Arrange
        var request = new CreateCategoryRequest
        {
            Name = "Alimentação",
            Color = "#FF5722"
        };

        // Act
        var result = await _service.CreateCategoryAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Alimentação");
        result.Color.Should().Be("#FF5722");
        result.Id.Should().NotBeEmpty();

        _mocks.CategoryRepository.Verify(r => r.CreateAsync(It.IsAny<Category>()), Times.Once);
    }

    [Fact]
    public async Task CreateCategoryAsync_ShouldReturnCategoryDto()
    {
        // Arrange
        var request = new CreateCategoryRequest
        {
            Name = "Transporte",
            Color = "#2196F3"
        };

        // Act
        var result = await _service.CreateCategoryAsync(request);

        // Assert
        result.Should().BeOfType<CategoryDto>();
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    #endregion

    #region GetAllCategoriesAsync Tests

    [Fact]
    public async Task GetAllCategoriesAsync_ShouldReturnAllCategories()
    {
        // Arrange
        var categories = new List<Category>
        {
            new Category("Alimentação", "#FF5722"),
            new Category("Transporte", "#2196F3"),
            new Category("Lazer", "#4CAF50")
        };
        _mocks.SetupAllCategories(categories);

        // Act
        var result = await _service.GetAllCategoriesAsync();

        // Assert
        result.Should().HaveCount(3);
        result.Select(c => c.Name).Should().Contain(new[] { "Alimentação", "Transporte", "Lazer" });
    }

    [Fact]
    public async Task GetAllCategoriesAsync_WhenEmpty_ShouldReturnEmptyList()
    {
        // Arrange
        _mocks.SetupAllCategories(new List<Category>());

        // Act
        var result = await _service.GetAllCategoriesAsync();

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region GetCategoryByIdAsync Tests

    [Fact]
    public async Task GetCategoryByIdAsync_WhenExists_ShouldReturnCategory()
    {
        // Arrange
        var category = new Category("Alimentação", "#FF5722");
        _mocks.SetupCategoryById(category);

        // Act
        var result = await _service.GetCategoryByIdAsync(category.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(category.Id);
        result.Name.Should().Be("Alimentação");
    }

    [Fact]
    public async Task GetCategoryByIdAsync_WhenNotExists_ShouldReturnNull()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        _mocks.CategoryRepository
            .Setup(r => r.GetByIdAsync(nonExistentId))
            .ReturnsAsync((Category?)null);

        // Act
        var result = await _service.GetCategoryByIdAsync(nonExistentId);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region UpdateCategoryAsync Tests

    [Fact]
    public async Task UpdateCategoryAsync_WithValidData_ShouldUpdateCategory()
    {
        // Arrange
        var category = new Category("Alimentação", "#FF5722");
        _mocks.SetupCategoryById(category);

        var request = new UpdateCategoryRequest
        {
            Name = "Alimentação Atualizada",
            Color = "#E91E63"
        };

        // Act
        var result = await _service.UpdateCategoryAsync(category.Id, request);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Alimentação Atualizada");
        result.Color.Should().Be("#E91E63");

        _mocks.CategoryRepository.Verify(r => r.UpdateAsync(category), Times.Once);
    }

    [Fact]
    public async Task UpdateCategoryAsync_WhenNotExists_ShouldReturnNull()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        _mocks.CategoryRepository
            .Setup(r => r.GetByIdAsync(nonExistentId))
            .ReturnsAsync((Category?)null);

        var request = new UpdateCategoryRequest
        {
            Name = "Teste",
            Color = "#000000"
        };

        // Act
        var result = await _service.UpdateCategoryAsync(nonExistentId, request);

        // Assert
        result.Should().BeNull();
        _mocks.CategoryRepository.Verify(r => r.UpdateAsync(It.IsAny<Category>()), Times.Never);
    }

    #endregion

    #region DeleteCategoryAsync Tests

    [Fact]
    public async Task DeleteCategoryAsync_WhenExistsAndNotInUse_ShouldDeleteAndReturnSuccess()
    {
        // Arrange
        var category = new Category("Alimentação", "#FF5722");
        _mocks.SetupCategoryById(category);
        _mocks.SetupCategoryInUse(category.Id, false);

        // Act
        var (success, errorMessage) = await _service.DeleteCategoryAsync(category.Id);

        // Assert
        success.Should().BeTrue();
        errorMessage.Should().BeNull();

        _mocks.CategoryRepository.Verify(r => r.DeleteAsync(category.Id), Times.Once);
    }

    [Fact]
    public async Task DeleteCategoryAsync_WhenNotExists_ShouldReturnFailure()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        _mocks.CategoryRepository
            .Setup(r => r.GetByIdAsync(nonExistentId))
            .ReturnsAsync((Category?)null);

        // Act
        var (success, errorMessage) = await _service.DeleteCategoryAsync(nonExistentId);

        // Assert
        success.Should().BeFalse();
        errorMessage.Should().Be("Categoria não encontrada");

        _mocks.CategoryRepository.Verify(r => r.DeleteAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task DeleteCategoryAsync_WhenInUse_ShouldReturnFailure()
    {
        // Arrange
        var category = new Category("Alimentação", "#FF5722");
        _mocks.SetupCategoryById(category);
        _mocks.SetupCategoryInUse(category.Id, true);

        // Act
        var (success, errorMessage) = await _service.DeleteCategoryAsync(category.Id);

        // Assert
        success.Should().BeFalse();
        errorMessage.Should().Be("Não é possível excluir uma categoria que está em uso");

        _mocks.CategoryRepository.Verify(r => r.DeleteAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task DeleteCategoryAsync_ShouldCheckIfInUseBeforeDeleting()
    {
        // Arrange
        var category = new Category("Alimentação", "#FF5722");
        _mocks.SetupCategoryById(category);
        _mocks.SetupCategoryInUse(category.Id, false);

        // Act
        await _service.DeleteCategoryAsync(category.Id);

        // Assert
        _mocks.CategoryRepository.Verify(r => r.IsInUseAsync(category.Id), Times.Once);
    }

    #endregion
}

