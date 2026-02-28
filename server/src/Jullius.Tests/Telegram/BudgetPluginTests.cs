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
/// Testes para o BudgetPlugin (ListBudgets, CreateBudget, GetBudgetUsage).
/// </summary>
public class BudgetPluginTests
{
    private readonly Mock<IBudgetRepository> _budgetRepoMock = new();
    private readonly BudgetPlugin _plugin;

    public BudgetPluginTests()
    {
        var budgetService = new BudgetService(_budgetRepoMock.Object);
        _plugin = new BudgetPlugin(
            budgetService,
            Mock.Of<ILogger<BudgetPlugin>>());
    }

    #region ListBudgets

    [Fact]
    public async Task ListBudgets_ShouldReturnFormattedList()
    {
        var budget1 = new Budget("Alimentação", 800m, 1, 2025);
        var budget2 = new Budget("Transporte", 400m, 1, 2025);
        var budgets = new List<Budget> { budget1, budget2 };
        _budgetRepoMock.Setup(r => r.GetByMonthAndYearAsync(1, 2025)).ReturnsAsync(budgets);
        _budgetRepoMock.Setup(r => r.GetUsedAmountAsync(budget1.Id)).ReturnsAsync(500m);
        _budgetRepoMock.Setup(r => r.GetUsedAmountAsync(budget2.Id)).ReturnsAsync(390m);

        var result = await _plugin.ListBudgetsAsync(1, 2025);

        Assert.Contains("Alimentação", result);
        Assert.Contains("Transporte", result);
        Assert.Contains("800,00", result);
    }

    [Fact]
    public async Task ListBudgets_ShouldReturnEmptyMessage()
    {
        _budgetRepoMock.Setup(r => r.GetByMonthAndYearAsync(2, 2025)).ReturnsAsync(Array.Empty<Budget>());

        var result = await _plugin.ListBudgetsAsync(2, 2025);

        Assert.Contains("Nenhum orçamento", result);
    }

    #endregion

    #region CreateBudget

    [Fact]
    public async Task CreateBudget_ShouldReturnConfirmation()
    {
        _budgetRepoMock
            .Setup(r => r.CreateAsync(It.IsAny<Budget>()))
            .ReturnsAsync((Budget b) => b);
        _budgetRepoMock
            .Setup(r => r.GetUsedAmountAsync(It.IsAny<Guid>()))
            .ReturnsAsync(0m);

        var result = await _plugin.CreateBudgetAsync("Alimentação", 800m, 1, 2025, "Orçamento mensal");

        Assert.Contains("✅", result);
        Assert.Contains("Alimentação", result);
        Assert.Contains("800,00", result);
    }

    [Fact]
    public async Task CreateBudget_WithoutDescription_ShouldWork()
    {
        _budgetRepoMock
            .Setup(r => r.CreateAsync(It.IsAny<Budget>()))
            .ReturnsAsync((Budget b) => b);
        _budgetRepoMock
            .Setup(r => r.GetUsedAmountAsync(It.IsAny<Guid>()))
            .ReturnsAsync(0m);

        var result = await _plugin.CreateBudgetAsync("Lazer", 200m, 3, 2025);

        Assert.Contains("✅", result);
        Assert.Contains("Lazer", result);
    }

    #endregion

    #region GetBudgetUsage

    [Fact]
    public async Task GetBudgetUsage_ShouldReturnDetailedUsage()
    {
        var budget = new Budget("Alimentação", 800m, 1, 2025);
        _budgetRepoMock.Setup(r => r.GetByMonthAndYearAsync(1, 2025)).ReturnsAsync(new[] { budget });
        _budgetRepoMock.Setup(r => r.GetUsedAmountAsync(budget.Id)).ReturnsAsync(500m);

        var result = await _plugin.GetBudgetUsageAsync("Alimentação", 1, 2025);

        Assert.Contains("Alimentação", result);
        Assert.Contains("800,00", result);
        Assert.Contains("500,00", result);
    }

    [Fact]
    public async Task GetBudgetUsage_ShouldReturnError_WhenNotFound()
    {
        _budgetRepoMock.Setup(r => r.GetByMonthAndYearAsync(1, 2025)).ReturnsAsync(Array.Empty<Budget>());

        var result = await _plugin.GetBudgetUsageAsync("Inexistente", 1, 2025);

        Assert.Contains("não encontrado", result.ToLowerInvariant());
    }

    #endregion
}
