using FluentAssertions;
using Jullius.Domain.Domain.Entities;
using Jullius.Domain.Domain.Repositories;
using Jullius.ServiceApi.Application.Services;
using Jullius.ServiceApi.Telegram.Plugins;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Jullius.Tests.Telegram;

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
        _transactionRepoMock
            .Setup(r => r.CreateAsync(It.IsAny<FinancialTransaction>()))
            .ReturnsAsync((FinancialTransaction transaction) => transaction);

        _transactionRepoMock
            .Setup(r => r.UpdateAsync(It.IsAny<FinancialTransaction>()))
            .Returns(Task.CompletedTask);

        _transactionRepoMock
            .Setup(r => r.DeleteAsync(It.IsAny<Guid>()))
            .Returns(Task.CompletedTask);

        _transactionRepoMock
            .Setup(r => r.GetAllAsync())
            .ReturnsAsync(Array.Empty<FinancialTransaction>());

        _categoryRepoMock
            .Setup(r => r.GetAllAsync())
            .ReturnsAsync(Array.Empty<Category>());

        var transactionService = new FinancialTransactionService(
            _transactionRepoMock.Object,
            _cardRepoMock.Object,
            _categoryRepoMock.Object,
            _serviceProviderMock.Object);

        var categoryResolutionService = new CategoryResolutionService(
            _categoryRepoMock.Object,
            _transactionRepoMock.Object);

        var transactionResolutionService = new TransactionResolutionService(
            _transactionRepoMock.Object);

        _plugin = new FinancialTransactionPlugin(
            transactionService,
            categoryResolutionService,
            transactionResolutionService,
            _categoryRepoMock.Object,
            _transactionRepoMock.Object,
            _budgetRepoMock.Object,
            Mock.Of<ILogger<FinancialTransactionPlugin>>());
    }

    [Fact]
    public async Task CreateExpense_ShouldRegisterAsPaidByDefault_WhenHistoryProvidesSafeCategory()
    {
        var category = new Category("Essenciais", "#FF5722");
        var previousTransaction = new FinancialTransaction(
            "Compra no Myatã",
            25m,
            new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc),
            TransactionType.PayableBill,
            category.Id,
            true);

        _categoryRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new[] { category });
        _transactionRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new[] { previousTransaction });

        var result = await _plugin.CreateExpenseAsync("Compra no Myatã", 19.56m);

        result.Should().Contain("✅ Despesa registrada!");
        result.Should().Contain("Status: ✅ Pago");

        _transactionRepoMock.Verify(
            r => r.CreateAsync(It.Is<FinancialTransaction>(transaction =>
                transaction.Description == "Compra no Myatã" &&
                transaction.CategoryId == category.Id &&
                transaction.IsPaid)),
            Times.Once);
    }

    [Fact]
    public async Task CreateExpense_ShouldAskForCategory_WhenNoSafeMatchExists()
    {
        var categories = new[]
        {
            new Category("Essenciais", "#FF5722"),
            new Category("Lazer", "#4CAF50")
        };

        _categoryRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(categories);

        var result = await _plugin.CreateExpenseAsync("Compra na Papelaria Central", 10m);

        result.Should().Contain("O que eu faço");
        result.Should().Contain("Essenciais");
        result.Should().Contain("Lazer");

        _transactionRepoMock.Verify(r => r.CreateAsync(It.IsAny<FinancialTransaction>()), Times.Never);
    }

    [Fact]
    public async Task CreateExpense_ShouldAskBeforeCreatingRequestedCategory_WhenCategoryDoesNotExist()
    {
        var existingCategory = new Category("Essenciais", "#FF5722");
        _categoryRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new[] { existingCategory });

        var result = await _plugin.CreateExpenseAsync("The Best Acai", 70.29m, "Alimentação");

        result.Should().Contain("Não encontrei a categoria \"Alimentação\"");
        result.Should().Contain("cadastro \"Alimentação\"");
        result.Should().Contain("Essenciais");

        _transactionRepoMock.Verify(r => r.CreateAsync(It.IsAny<FinancialTransaction>()), Times.Never);
    }

    [Fact]
    public async Task CreateIncome_ShouldRespectExplicitPendingStatus()
    {
        var category = new Category("Freelance", "#607D8B");
        _categoryRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new[] { category });

        var result = await _plugin.CreateIncomeAsync("Projeto web", 2000m, "Freelance", false);

        result.Should().Contain("✅ Receita registrada!");
        result.Should().Contain("Status: ⏳ Pendente");

        _transactionRepoMock.Verify(
            r => r.CreateAsync(It.Is<FinancialTransaction>(transaction =>
                transaction.CategoryId == category.Id &&
                !transaction.IsPaid)),
            Times.Once);
    }

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

        result.Should().Contain("RECEITAS");
        result.Should().Contain("DESPESAS");
        result.Should().Contain("SALDO");
        result.Should().Contain("5.000,00");
        result.Should().Contain("1.500,00");
    }

    [Fact]
    public async Task SearchTransactions_ShouldMatchIgnoringDiacritics()
    {
        var categoryId = Guid.NewGuid();
        var transactions = new List<FinancialTransaction>
        {
            new("Myatã", 44.10m, new DateTime(2026, 2, 14, 0, 0, 0, DateTimeKind.Utc), TransactionType.PayableBill, categoryId, true),
            new("Myata", 22.50m, new DateTime(2026, 2, 14, 0, 0, 0, DateTimeKind.Utc), TransactionType.PayableBill, categoryId, true),
            new("Myatã", 13.02m, new DateTime(2026, 2, 20, 0, 0, 0, DateTimeKind.Utc), TransactionType.PayableBill, categoryId, true)
        };

        _transactionRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(transactions);

        var result = await _plugin.SearchTransactionsAsync("myatã", 2, 2026);

        result.Should().Contain("3 encontradas");
        result.Should().Contain("79,62");
    }

    [Fact]
    public async Task UpdatePaymentStatus_ShouldUpdateTransaction_WhenMatchIsUnique()
    {
        var transaction = new FinancialTransaction(
            "Conta de luz",
            250m,
            DateTime.UtcNow,
            TransactionType.PayableBill,
            Guid.NewGuid());

        _transactionRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new[] { transaction });
        _transactionRepoMock.Setup(r => r.GetByIdAsync(transaction.Id)).ReturnsAsync(transaction);

        var result = await _plugin.UpdatePaymentStatusAsync("conta de luz", true);

        result.Should().Contain("✅ Pago");
        result.Should().Contain("Conta de luz");
        _transactionRepoMock.Verify(r => r.UpdateAsync(transaction), Times.Once);
    }

    [Fact]
    public async Task UpdatePaymentStatus_ShouldAsk_WhenMatchIsAmbiguous()
    {
        var transactions = new[]
        {
            new FinancialTransaction("Conta de luz casa", 120m, new DateTime(2026, 4, 10, 0, 0, 0, DateTimeKind.Utc), TransactionType.PayableBill, Guid.NewGuid()),
            new FinancialTransaction("Conta de luz escritório", 220m, new DateTime(2026, 4, 11, 0, 0, 0, DateTimeKind.Utc), TransactionType.PayableBill, Guid.NewGuid())
        };

        _transactionRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(transactions);

        var result = await _plugin.UpdatePaymentStatusAsync("conta de luz", true);

        result.Should().Contain("mais de uma transação parecida");
        _transactionRepoMock.Verify(r => r.UpdateAsync(It.IsAny<FinancialTransaction>()), Times.Never);
    }

    [Fact]
    public async Task UpdateTransaction_ShouldUpdateCategory_WhenMatchIsUnique()
    {
        var oldCategory = new Category("Alimentação", "#FF5722");
        var newCategory = new Category("Não planejado", "#4CAF50");
        var transaction = new FinancialTransaction(
            "The Best Acai Lages Bra",
            70.29m,
            new DateTime(2026, 4, 3, 0, 0, 0, DateTimeKind.Utc),
            TransactionType.PayableBill,
            oldCategory.Id,
            true);

        _categoryRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new[] { oldCategory, newCategory });
        _transactionRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new[] { transaction });
        _transactionRepoMock.Setup(r => r.GetByIdAsync(transaction.Id)).ReturnsAsync(transaction);

        var result = await _plugin.UpdateTransactionAsync(
            "The Best Acai",
            newCategoryName: "Não planejado",
            currentAmount: 70.29m,
            currentDueDate: "2026-04-03");

        result.Should().Contain("✅ Despesa atualizada!");
        result.Should().Contain("Não planejado");

        _transactionRepoMock.Verify(
            r => r.UpdateAsync(It.Is<FinancialTransaction>(updated =>
                updated.CategoryId == newCategory.Id &&
                updated.Description == "The Best Acai Lages Bra")),
            Times.Once);
    }

    [Fact]
    public async Task DeleteTransaction_ShouldDeleteTransaction_WhenMatchIsUnique()
    {
        var transaction = new FinancialTransaction(
            "The Best Acai Lages Bra",
            70.29m,
            new DateTime(2026, 4, 3, 0, 0, 0, DateTimeKind.Utc),
            TransactionType.PayableBill,
            Guid.NewGuid(),
            true);

        _transactionRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new[] { transaction });
        _transactionRepoMock.Setup(r => r.GetByIdAsync(transaction.Id)).ReturnsAsync(transaction);

        var result = await _plugin.DeleteTransactionAsync(
            "The Best Acai",
            currentAmount: 70.29m,
            currentDueDate: "2026-04-03");

        result.Should().Contain("✅ Transação excluída");
        _transactionRepoMock.Verify(r => r.DeleteAsync(transaction.Id), Times.Once);
    }

    [Fact]
    public async Task DeleteTransaction_ShouldAsk_WhenMatchIsAmbiguous()
    {
        var transactions = new[]
        {
            new FinancialTransaction("Myatã Centro", 19.56m, new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc), TransactionType.PayableBill, Guid.NewGuid(), true),
            new FinancialTransaction("Myatã Mercado", 19.56m, new DateTime(2026, 4, 2, 0, 0, 0, DateTimeKind.Utc), TransactionType.PayableBill, Guid.NewGuid(), true)
        };

        _transactionRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(transactions);

        var result = await _plugin.DeleteTransactionAsync("Myatã", currentAmount: 19.56m);

        result.Should().Contain("mais de uma transação parecida");
        _transactionRepoMock.Verify(r => r.DeleteAsync(It.IsAny<Guid>()), Times.Never);
    }
}
