using FluentAssertions;
using Jullius.Domain.Domain.Entities;
using Jullius.Domain.Domain.Repositories;
using Jullius.ServiceApi.Application.DTOs;
using Jullius.ServiceApi.Application.Services;
using Jullius.ServiceApi.Telegram.Plugins;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Jullius.Tests.Telegram;

/// <summary>
/// Testes para o FinancialTransactionPlugin (CreateExpense, CreateIncome, GetMonthlySummary, UpdatePaymentStatus).
/// </summary>
public class FinancialTransactionPluginTests
{
    private readonly Mock<ICategoryRepository> _categoryRepoMock = new();
    private readonly Mock<IFinancialTransactionRepository> _transactionRepoMock = new();
    private readonly Mock<IBudgetRepository> _budgetRepoMock = new();
    private readonly Mock<ICardRepository> _cardRepoMock = new();
    private readonly Mock<IServiceProvider> _serviceProviderMock = new();
    private readonly FinancialTransactionPlugin _plugin;

    public FinancialTransactionPluginTests()
    {
        // Create real service with mocked repositories
        _transactionRepoMock
            .Setup(r => r.CreateAsync(It.IsAny<FinancialTransaction>()))
            .ReturnsAsync((FinancialTransaction ft) => ft);

        var transactionService = new FinancialTransactionService(
            _transactionRepoMock.Object,
            _cardRepoMock.Object,
            _categoryRepoMock.Object,
            _serviceProviderMock.Object);

        _plugin = new FinancialTransactionPlugin(
            transactionService,
            _categoryRepoMock.Object,
            _transactionRepoMock.Object,
            _budgetRepoMock.Object,
            Mock.Of<ILogger<FinancialTransactionPlugin>>());
    }

    #region CreateExpense

    [Fact]
    public async Task CreateExpense_ShouldReturnConfirmation_WhenCategoryExists()
    {
        var category = new Category("Alimentação", "#FF5722");
        _categoryRepoMock.Setup(r => r.GetByNameAsync("Alimentação")).ReturnsAsync(category);

        var result = await _plugin.CreateExpenseAsync("Almoço", 35.50m, "Alimentação", false);

        Assert.Contains("✅ Despesa registrada!", result);
        Assert.Contains("Almoço", result);
        Assert.Contains("35,50", result);
        Assert.Contains("Alimentação", result);
    }

    [Fact]
    public async Task CreateExpense_ShouldAutoCreateCategory_WhenNotFound()
    {
        _categoryRepoMock.Setup(r => r.GetByNameAsync("Nova")).ReturnsAsync((Category?)null);
        _categoryRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(Array.Empty<Category>());
        var newCategory = new Category("Nova", "#4CAF50");
        _categoryRepoMock
            .Setup(r => r.GetOrCreateSystemCategoryAsync("Nova", It.IsAny<string>()))
            .ReturnsAsync(newCategory);

        var result = await _plugin.CreateExpenseAsync("Teste", 10m, "Nova", false);

        Assert.Contains("✅ Despesa registrada!", result);
        _categoryRepoMock.Verify(r => r.GetOrCreateSystemCategoryAsync("Nova", It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task CreateExpense_Paid_ShouldShowCheckMark()
    {
        var category = new Category("Saúde", "#4CAF50");
        _categoryRepoMock.Setup(r => r.GetByNameAsync("Saúde")).ReturnsAsync(category);

        var result = await _plugin.CreateExpenseAsync("Farmácia", 50m, "Saúde", true);

        Assert.Contains("✅", result);
    }

    [Fact]
    public async Task CreateExpense_WithDueDate_ShouldParseDate()
    {
        var category = new Category("Moradia", "#FF5722");
        _categoryRepoMock.Setup(r => r.GetByNameAsync("Moradia")).ReturnsAsync(category);

        var result = await _plugin.CreateExpenseAsync("Aluguel", 1500m, "Moradia", false, "2025-02-15");

        Assert.Contains("✅ Despesa registrada!", result);
    }

    #endregion

    #region CreateIncome

    [Fact]
    public async Task CreateIncome_ShouldReturnConfirmation()
    {
        var category = new Category("Salário", "#4CAF50");
        _categoryRepoMock.Setup(r => r.GetByNameAsync("Salário")).ReturnsAsync(category);

        var result = await _plugin.CreateIncomeAsync("Salário janeiro", 5000m, "Salário", true);

        Assert.Contains("✅ Receita registrada!", result);
        Assert.Contains("5.000,00", result);
        Assert.Contains("Recebido", result);
    }

    [Fact]
    public async Task CreateIncome_Pending_ShouldShowPendingLabel()
    {
        var category = new Category("Freelance", "#607D8B");
        _categoryRepoMock.Setup(r => r.GetByNameAsync("Freelance")).ReturnsAsync(category);

        var result = await _plugin.CreateIncomeAsync("Projeto web", 2000m, "Freelance", false);

        Assert.Contains("Pendente", result);
    }

    #endregion

    #region GetMonthlySummary

    [Fact]
    public async Task GetMonthlySummary_ShouldReturnFormattedSummary()
    {
        var categoryId = Guid.NewGuid();
        var transactions = new List<FinancialTransaction>
        {
            new("Salário", 5000m, new DateTime(2025, 1, 5, 0, 0, 0, DateTimeKind.Utc), TransactionType.ReceivableBill, categoryId, true),
            new("Aluguel", 1500m, new DateTime(2025, 1, 10, 0, 0, 0, DateTimeKind.Utc), TransactionType.PayableBill, categoryId, true)
        };

        _transactionRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(transactions);
        _budgetRepoMock.Setup(r => r.GetByMonthAndYearAsync(1, 2025)).ReturnsAsync(Array.Empty<Budget>());

        var result = await _plugin.GetMonthlySummaryAsync(1, 2025);

        Assert.Contains("RECEITAS", result);
        Assert.Contains("DESPESAS", result);
        Assert.Contains("SALDO", result);
        Assert.Contains("5.000,00", result);
        Assert.Contains("1.500,00", result);
    }

    [Fact]
    public async Task GetMonthlySummary_WithBudgets_ShouldShowBudgetUsage()
    {
        var categoryId = Guid.NewGuid();
        var budget = new Budget("Alimentação", 800m, 1, 2025);
        var transactions = new List<FinancialTransaction>
        {
            new("Almoço", 500m, new DateTime(2025, 1, 15, 0, 0, 0, DateTimeKind.Utc), TransactionType.PayableBill, categoryId, true, null, budget.Id)
        };

        _transactionRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(transactions);
        _budgetRepoMock.Setup(r => r.GetByMonthAndYearAsync(1, 2025)).ReturnsAsync(new[] { budget });

        var result = await _plugin.GetMonthlySummaryAsync(1, 2025);

        Assert.Contains("ORÇAMENTOS", result);
        Assert.Contains("Alimentação", result);
    }

    #endregion

    #region UpdatePaymentStatus

    [Fact]
    public async Task UpdatePaymentStatus_ShouldFindByDescription()
    {
        var transaction = new FinancialTransaction("Conta de luz", 250m, DateTime.UtcNow, TransactionType.PayableBill, Guid.NewGuid());

        _transactionRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new[] { transaction });

        var result = await _plugin.UpdatePaymentStatusAsync("conta de luz", true);

        Assert.Contains("✅ Pago", result);
        Assert.Contains("Conta de luz", result);
    }

    [Fact]
    public async Task UpdatePaymentStatus_ShouldReturnError_WhenNotFound()
    {
        _transactionRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(Array.Empty<FinancialTransaction>());

        var result = await _plugin.UpdatePaymentStatusAsync("inexistente", true);

        Assert.Contains("❌", result);
        Assert.Contains("não encontrei", result.ToLowerInvariant());
    }

    #endregion
}
