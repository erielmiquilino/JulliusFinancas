using FluentAssertions;
using Jullius.Domain.Domain.Entities;
using Jullius.ServiceApi.Application.DTOs;
using Jullius.ServiceApi.Application.Services;
using Jullius.Tests.Mocks;
using Moq;
using Xunit;

namespace Jullius.Tests.Services;

public class BudgetServiceTests
{
    private readonly RepositoryMocks _mocks;
    private readonly BudgetService _service;

    public BudgetServiceTests()
    {
        _mocks = new RepositoryMocks();
        _service = new BudgetService(_mocks.BudgetRepository.Object);
    }

    #region CreateBudgetAsync Tests

    [Fact]
    public async Task CreateBudgetAsync_WithValidData_ShouldCreateBudget()
    {
        // Arrange
        var request = new CreateBudgetRequest
        {
            Name = "Alimentação",
            LimitAmount = 1000m,
            Month = 6,
            Year = 2025,
            Description = "Budget para alimentação"
        };

        // Act
        var result = await _service.CreateBudgetAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Alimentação");
        result.LimitAmount.Should().Be(1000m);
        result.Month.Should().Be(6);
        result.Year.Should().Be(2025);
        result.Description.Should().Be("Budget para alimentação");
        result.UsedAmount.Should().Be(0);
        result.RemainingAmount.Should().Be(1000m);
        result.UsagePercentage.Should().Be(0);

        _mocks.BudgetRepository.Verify(r => r.CreateAsync(It.IsAny<Budget>()), Times.Once);
    }

    [Fact]
    public async Task CreateBudgetAsync_WithInvalidData_ShouldThrowArgumentException()
    {
        // Arrange
        var request = new CreateBudgetRequest
        {
            Name = "",
            LimitAmount = 1000m,
            Month = 6,
            Year = 2025
        };

        // Act
        var act = async () => await _service.CreateBudgetAsync(request);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Name cannot be empty");
    }

    #endregion

    #region GetAllBudgetsAsync Tests

    [Fact]
    public async Task GetAllBudgetsAsync_ShouldReturnAllBudgets()
    {
        // Arrange
        var budgets = new List<Budget>
        {
            new Budget("Budget 1", 1000m, 6, 2025),
            new Budget("Budget 2", 2000m, 7, 2025)
        };
        _mocks.SetupAllBudgets(budgets);

        // Act
        var result = await _service.GetAllBudgetsAsync();

        // Assert
        result.Should().HaveCount(2);
    }

    #endregion

    #region GetBudgetsByMonthAndYearAsync Tests

    [Fact]
    public async Task GetBudgetsByMonthAndYearAsync_ShouldReturnBudgetsForPeriod()
    {
        // Arrange
        var budgets = new List<Budget>
        {
            new Budget("Budget 1", 1000m, 6, 2025),
            new Budget("Budget 2", 2000m, 6, 2025)
        };
        _mocks.SetupBudgetsByMonthAndYear(6, 2025, budgets);

        // Act
        var result = await _service.GetBudgetsByMonthAndYearAsync(6, 2025);

        // Assert
        result.Should().HaveCount(2);
    }

    #endregion

    #region GetBudgetByIdAsync Tests

    [Fact]
    public async Task GetBudgetByIdAsync_WhenExists_ShouldReturnBudget()
    {
        // Arrange
        var budget = new Budget("Budget", 1000m, 6, 2025);
        _mocks.SetupBudgetById(budget);

        // Act
        var result = await _service.GetBudgetByIdAsync(budget.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(budget.Id);
    }

    [Fact]
    public async Task GetBudgetByIdAsync_WhenNotExists_ShouldReturnNull()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        _mocks.BudgetRepository
            .Setup(r => r.GetByIdAsync(nonExistentId))
            .ReturnsAsync((Budget?)null);

        // Act
        var result = await _service.GetBudgetByIdAsync(nonExistentId);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region UpdateBudgetAsync Tests

    [Fact]
    public async Task UpdateBudgetAsync_WithValidData_ShouldUpdateBudget()
    {
        // Arrange
        var budget = new Budget("Budget Original", 1000m, 6, 2025);
        _mocks.SetupBudgetById(budget);

        var request = new UpdateBudgetRequest
        {
            Name = "Budget Atualizado",
            LimitAmount = 2000m,
            Month = 7,
            Year = 2025,
            Description = "Nova descrição"
        };

        // Act
        var result = await _service.UpdateBudgetAsync(budget.Id, request);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Budget Atualizado");
        result.LimitAmount.Should().Be(2000m);
        result.Month.Should().Be(7);
        result.Description.Should().Be("Nova descrição");

        _mocks.BudgetRepository.Verify(r => r.UpdateAsync(budget), Times.Once);
    }

    [Fact]
    public async Task UpdateBudgetAsync_WhenNotFound_ShouldReturnNull()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        _mocks.BudgetRepository
            .Setup(r => r.GetByIdAsync(nonExistentId))
            .ReturnsAsync((Budget?)null);

        var request = new UpdateBudgetRequest
        {
            Name = "Budget",
            LimitAmount = 1000m,
            Month = 6,
            Year = 2025
        };

        // Act
        var result = await _service.UpdateBudgetAsync(nonExistentId, request);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region DeleteBudgetAsync Tests

    [Fact]
    public async Task DeleteBudgetAsync_WhenNotInUse_ShouldDeleteAndReturnSuccess()
    {
        // Arrange
        var budget = new Budget("Budget", 1000m, 6, 2025);
        _mocks.SetupBudgetById(budget);
        _mocks.SetupBudgetInUse(budget.Id, false);

        // Act
        var (success, errorMessage) = await _service.DeleteBudgetAsync(budget.Id);

        // Assert
        success.Should().BeTrue();
        errorMessage.Should().BeNull();
        _mocks.BudgetRepository.Verify(r => r.DeleteAsync(budget.Id), Times.Once);
    }

    [Fact]
    public async Task DeleteBudgetAsync_WhenInUse_ShouldReturnError()
    {
        // Arrange
        var budget = new Budget("Budget", 1000m, 6, 2025);
        _mocks.SetupBudgetById(budget);
        _mocks.SetupBudgetInUse(budget.Id, true);

        // Act
        var (success, errorMessage) = await _service.DeleteBudgetAsync(budget.Id);

        // Assert
        success.Should().BeFalse();
        errorMessage.Should().Be("Não é possível excluir um budget que possui transações vinculadas");
        _mocks.BudgetRepository.Verify(r => r.DeleteAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task DeleteBudgetAsync_WhenNotFound_ShouldReturnError()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        _mocks.BudgetRepository
            .Setup(r => r.GetByIdAsync(nonExistentId))
            .ReturnsAsync((Budget?)null);

        // Act
        var (success, errorMessage) = await _service.DeleteBudgetAsync(nonExistentId);

        // Assert
        success.Should().BeFalse();
        errorMessage.Should().Be("Budget não encontrado");
    }

    #endregion

    #region Usage Calculation Tests

    [Fact]
    public async Task GetBudgetByIdAsync_ShouldCalculateUsageCorrectly()
    {
        // Arrange
        var budget = new Budget("Budget", 1000m, 6, 2025);
        _mocks.SetupBudgetById(budget);
        _mocks.SetupBudgetUsedAmount(budget.Id, 750m);

        // Act
        var result = await _service.GetBudgetByIdAsync(budget.Id);

        // Assert
        result.Should().NotBeNull();
        result!.LimitAmount.Should().Be(1000m);
        result.UsedAmount.Should().Be(750m);
        result.RemainingAmount.Should().Be(250m);
        result.UsagePercentage.Should().Be(75m);
    }

    [Fact]
    public async Task GetBudgetByIdAsync_WhenOverBudget_ShouldShowNegativeRemaining()
    {
        // Arrange
        var budget = new Budget("Budget", 1000m, 6, 2025);
        _mocks.SetupBudgetById(budget);
        _mocks.SetupBudgetUsedAmount(budget.Id, 1200m);

        // Act
        var result = await _service.GetBudgetByIdAsync(budget.Id);

        // Assert
        result.Should().NotBeNull();
        result!.UsedAmount.Should().Be(1200m);
        result.RemainingAmount.Should().Be(-200m);
        result.UsagePercentage.Should().Be(120m);
    }

    [Fact]
    public async Task GetBudgetByIdAsync_WhenNoTransactions_ShouldShowZeroUsage()
    {
        // Arrange
        var budget = new Budget("Budget", 1000m, 6, 2025);
        _mocks.SetupBudgetById(budget);
        _mocks.SetupBudgetUsedAmount(budget.Id, 0m);

        // Act
        var result = await _service.GetBudgetByIdAsync(budget.Id);

        // Assert
        result.Should().NotBeNull();
        result!.UsedAmount.Should().Be(0m);
        result.RemainingAmount.Should().Be(1000m);
        result.UsagePercentage.Should().Be(0m);
    }

    #endregion

    #region Color Threshold Tests (Frontend logic, but validating DTO values)

    [Fact]
    public async Task GetBudgetByIdAsync_Under70Percent_ShouldBeGreenZone()
    {
        // Arrange
        var budget = new Budget("Budget", 1000m, 6, 2025);
        _mocks.SetupBudgetById(budget);
        _mocks.SetupBudgetUsedAmount(budget.Id, 500m); // 50%

        // Act
        var result = await _service.GetBudgetByIdAsync(budget.Id);

        // Assert
        result!.UsagePercentage.Should().BeLessThan(70m);
    }

    [Fact]
    public async Task GetBudgetByIdAsync_Between70And90Percent_ShouldBeYellowZone()
    {
        // Arrange
        var budget = new Budget("Budget", 1000m, 6, 2025);
        _mocks.SetupBudgetById(budget);
        _mocks.SetupBudgetUsedAmount(budget.Id, 800m); // 80%

        // Act
        var result = await _service.GetBudgetByIdAsync(budget.Id);

        // Assert
        result!.UsagePercentage.Should().BeGreaterThanOrEqualTo(70m);
        result.UsagePercentage.Should().BeLessThanOrEqualTo(90m);
    }

    [Fact]
    public async Task GetBudgetByIdAsync_Above90Percent_ShouldBeRedZone()
    {
        // Arrange
        var budget = new Budget("Budget", 1000m, 6, 2025);
        _mocks.SetupBudgetById(budget);
        _mocks.SetupBudgetUsedAmount(budget.Id, 950m); // 95%

        // Act
        var result = await _service.GetBudgetByIdAsync(budget.Id);

        // Assert
        result!.UsagePercentage.Should().BeGreaterThan(90m);
    }

    #endregion
}

