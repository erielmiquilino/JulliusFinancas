using FluentAssertions;
using Jullius.Domain.Domain.Entities;
using Jullius.ServiceApi.Application.DTOs;
using Jullius.ServiceApi.Application.Services;
using Jullius.Tests.Mocks;
using Moq;
using Xunit;

namespace Jullius.Tests.Services;

public class CardTransactionServiceTests
{
    private readonly RepositoryMocks _mocks;
    private readonly CardTransactionService _service;
    private readonly Card _testCard;
    private readonly Category _testCategory;

    public CardTransactionServiceTests()
    {
        _mocks = new RepositoryMocks();
        _service = new CardTransactionService(
            _mocks.CardTransactionRepository.Object,
            _mocks.CardRepository.Object,
            _mocks.FinancialTransactionRepository.Object,
            _mocks.CategoryRepository.Object
        );

        // Setup dados de teste comuns
        _testCard = new Card("Nubank", "Nubank", 15, 22, 5000m);
        _testCategory = new Category("Fatura de Cartão", "#E91E63");

        _mocks.SetupCardById(_testCard);
        _mocks.SetupSystemCategory(_testCategory);
    }

    #region CreateCardTransactionAsync Tests

    [Fact]
    public async Task CreateCardTransactionAsync_WithSingleTransaction_ShouldCreateOneTransaction()
    {
        // Arrange
        _mocks.SetupInvoiceByCardAndPeriod(_testCard.Id, 2025, 6, null);

        var request = new CreateCardTransactionRequest
        {
            CardId = _testCard.Id,
            Description = "Compra Amazon",
            Amount = 150m,
            Date = DateTime.UtcNow,
            IsInstallment = false,
            InstallmentCount = 1,
            Type = CardTransactionType.Expense,
            InvoiceYear = 2025,
            InvoiceMonth = 6
        };

        // Act
        var result = await _service.CreateCardTransactionAsync(request);

        // Assert
        result.Should().HaveCount(1);
        var transaction = result.First();
        transaction.Description.Should().Be("Compra Amazon");
        transaction.Amount.Should().Be(150m);
        transaction.Installment.Should().Be("1/1");
        transaction.Type.Should().Be(CardTransactionType.Expense);

        _mocks.CardTransactionRepository.Verify(r => r.CreateAsync(It.IsAny<CardTransaction>()), Times.Once);
        _mocks.CardRepository.Verify(r => r.UpdateAsync(It.IsAny<Card>()), Times.Once);
    }

    [Fact]
    public async Task CreateCardTransactionAsync_WithInstallments_ShouldCreateMultipleTransactions()
    {
        // Arrange
        _mocks.SetupInvoiceByCardAndPeriod(_testCard.Id, 2025, 6, null);
        _mocks.SetupInvoiceByCardAndPeriod(_testCard.Id, 2025, 7, null);
        _mocks.SetupInvoiceByCardAndPeriod(_testCard.Id, 2025, 8, null);

        var request = new CreateCardTransactionRequest
        {
            CardId = _testCard.Id,
            Description = "Compra Parcelada",
            Amount = 300m,
            Date = DateTime.UtcNow,
            IsInstallment = true,
            InstallmentCount = 3,
            Type = CardTransactionType.Expense,
            InvoiceYear = 2025,
            InvoiceMonth = 6
        };

        // Act
        var result = await _service.CreateCardTransactionAsync(request);

        // Assert
        result.Should().HaveCount(3);
        
        var transactions = result.ToList();
        transactions[0].Installment.Should().Be("1/3");
        transactions[1].Installment.Should().Be("2/3");
        transactions[2].Installment.Should().Be("3/3");

        // Cada parcela deve ter valor de 100
        transactions.All(t => t.Amount == 100m).Should().BeTrue();

        _mocks.CardTransactionRepository.Verify(r => r.CreateAsync(It.IsAny<CardTransaction>()), Times.Exactly(3));
    }

    [Fact]
    public async Task CreateCardTransactionAsync_WithInstallments_ShouldIncrementInvoiceMonths()
    {
        // Arrange
        _mocks.SetupInvoiceByCardAndPeriod(_testCard.Id, 2025, 11, null);
        _mocks.SetupInvoiceByCardAndPeriod(_testCard.Id, 2025, 12, null);
        _mocks.SetupInvoiceByCardAndPeriod(_testCard.Id, 2026, 1, null);

        var request = new CreateCardTransactionRequest
        {
            CardId = _testCard.Id,
            Description = "Compra Virada de Ano",
            Amount = 300m,
            Date = new DateTime(2025, 11, 1, 0, 0, 0, DateTimeKind.Utc),
            IsInstallment = true,
            InstallmentCount = 3,
            Type = CardTransactionType.Expense,
            InvoiceYear = 2025,
            InvoiceMonth = 11
        };

        // Act
        var result = await _service.CreateCardTransactionAsync(request);

        // Assert
        var transactions = result.ToList();
        transactions[0].InvoiceYear.Should().Be(2025);
        transactions[0].InvoiceMonth.Should().Be(11);
        transactions[1].InvoiceYear.Should().Be(2025);
        transactions[1].InvoiceMonth.Should().Be(12);
        transactions[2].InvoiceYear.Should().Be(2026);
        transactions[2].InvoiceMonth.Should().Be(1);
    }

    [Fact]
    public async Task CreateCardTransactionAsync_WhenCardNotFound_ShouldThrowArgumentException()
    {
        // Arrange
        var nonExistentCardId = Guid.NewGuid();
        _mocks.CardRepository
            .Setup(r => r.GetByIdAsync(nonExistentCardId))
            .ReturnsAsync((Card?)null);

        var request = new CreateCardTransactionRequest
        {
            CardId = nonExistentCardId,
            Description = "Compra",
            Amount = 100m,
            Date = DateTime.UtcNow,
            InvoiceYear = 2025,
            InvoiceMonth = 6
        };

        // Act
        var act = async () => await _service.CreateCardTransactionAsync(request);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Card not found");
    }

    [Fact]
    public async Task CreateCardTransactionAsync_WithExpense_ShouldDecreaseCurrentLimit()
    {
        // Arrange
        var initialLimit = _testCard.CurrentLimit;
        _mocks.SetupInvoiceByCardAndPeriod(_testCard.Id, 2025, 6, null);

        var request = new CreateCardTransactionRequest
        {
            CardId = _testCard.Id,
            Description = "Compra",
            Amount = 500m,
            Date = DateTime.UtcNow,
            IsInstallment = false,
            Type = CardTransactionType.Expense,
            InvoiceYear = 2025,
            InvoiceMonth = 6
        };

        // Act
        await _service.CreateCardTransactionAsync(request);

        // Assert
        _testCard.CurrentLimit.Should().Be(initialLimit - 500m);
    }

    [Fact]
    public async Task CreateCardTransactionAsync_WithIncome_ShouldIncreaseCurrentLimit()
    {
        // Arrange
        _testCard.UpdateCurrentLimit(-1000m); // Simula uso prévio do cartão
        var limitAfterUsage = _testCard.CurrentLimit;
        
        // Uma fatura existente para que o estorno possa ser subtraído
        var existingInvoice = new FinancialTransaction(
            "Fatura Nubank", 500m, DateTime.UtcNow, TransactionType.PayableBill, _testCategory.Id, cardId: _testCard.Id);
        _mocks.SetupInvoiceByCardAndPeriod(_testCard.Id, 2025, 6, existingInvoice);

        var request = new CreateCardTransactionRequest
        {
            CardId = _testCard.Id,
            Description = "Estorno",
            Amount = 300m,
            Date = DateTime.UtcNow,
            IsInstallment = false,
            Type = CardTransactionType.Income,
            InvoiceYear = 2025,
            InvoiceMonth = 6
        };

        // Act
        await _service.CreateCardTransactionAsync(request);

        // Assert
        _testCard.CurrentLimit.Should().Be(limitAfterUsage + 300m);
    }

    [Fact]
    public async Task CreateCardTransactionAsync_ShouldCreateNewInvoiceWhenNotExists()
    {
        // Arrange
        _mocks.SetupInvoiceByCardAndPeriod(_testCard.Id, 2025, 6, null);

        var request = new CreateCardTransactionRequest
        {
            CardId = _testCard.Id,
            Description = "Compra",
            Amount = 200m,
            Date = DateTime.UtcNow,
            Type = CardTransactionType.Expense,
            InvoiceYear = 2025,
            InvoiceMonth = 6
        };

        // Act
        await _service.CreateCardTransactionAsync(request);

        // Assert
        _mocks.FinancialTransactionRepository.Verify(
            r => r.CreateAsync(It.Is<FinancialTransaction>(ft => 
                ft.Description.Contains("Fatura") && 
                ft.Amount == 200m)),
            Times.Once);
    }

    [Fact]
    public async Task CreateCardTransactionAsync_ShouldUpdateExistingInvoice()
    {
        // Arrange
        var existingInvoice = new FinancialTransaction(
            "Fatura Nubank", 500m, DateTime.UtcNow, TransactionType.PayableBill, _testCategory.Id, cardId: _testCard.Id);
        _mocks.SetupInvoiceByCardAndPeriod(_testCard.Id, 2025, 6, existingInvoice);

        var request = new CreateCardTransactionRequest
        {
            CardId = _testCard.Id,
            Description = "Nova Compra",
            Amount = 200m,
            Date = DateTime.UtcNow,
            Type = CardTransactionType.Expense,
            InvoiceYear = 2025,
            InvoiceMonth = 6
        };

        // Act
        await _service.CreateCardTransactionAsync(request);

        // Assert
        existingInvoice.Amount.Should().Be(700m); // 500 + 200
        _mocks.FinancialTransactionRepository.Verify(r => r.UpdateAsync(existingInvoice), Times.Once);
    }

    #endregion

    #region GetCardTransactionsForInvoiceAsync Tests

    [Fact]
    public async Task GetCardTransactionsForInvoiceAsync_ShouldReturnInvoiceData()
    {
        // Arrange
        var transactions = new List<CardTransaction>
        {
            new CardTransaction(_testCard.Id, "Compra 1", 100m, DateTime.UtcNow, "1/1", 2025, 6, CardTransactionType.Expense),
            new CardTransaction(_testCard.Id, "Compra 2", 200m, DateTime.UtcNow, "1/1", 2025, 6, CardTransactionType.Expense),
            new CardTransaction(_testCard.Id, "Estorno", 50m, DateTime.UtcNow, "1/1", 2025, 6, CardTransactionType.Income)
        };
        _mocks.SetupCardTransactionsByCardAndPeriod(_testCard.Id, 6, 2025, transactions);

        // Act
        var result = await _service.GetCardTransactionsForInvoiceAsync(_testCard.Id, 6, 2025);

        // Assert
        result.Should().NotBeNull();
        result.CardName.Should().Be("Nubank");
        result.Month.Should().Be(6);
        result.Year.Should().Be(2025);
        result.InvoiceTotal.Should().Be(250m); // 100 + 200 - 50
        result.Transactions.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetCardTransactionsForInvoiceAsync_WhenCardNotFound_ShouldThrowArgumentException()
    {
        // Arrange
        var nonExistentCardId = Guid.NewGuid();
        _mocks.CardRepository
            .Setup(r => r.GetByIdAsync(nonExistentCardId))
            .ReturnsAsync((Card?)null);
        _mocks.CardTransactionRepository
            .Setup(r => r.GetByCardIdAndPeriodAsync(nonExistentCardId, It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(new List<CardTransaction>());

        // Act
        var act = async () => await _service.GetCardTransactionsForInvoiceAsync(nonExistentCardId, 6, 2025);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Card not found");
    }

    #endregion

    #region UpdateCardTransactionAsync Tests

    [Fact]
    public async Task UpdateCardTransactionAsync_WithValidData_ShouldUpdateTransaction()
    {
        // Arrange
        var transaction = new CardTransaction(_testCard.Id, "Compra Original", 100m, DateTime.UtcNow, "1/1", 2025, 6, CardTransactionType.Expense);
        _mocks.SetupCardTransactionById(transaction);
        _mocks.SetupInvoiceByCardAndPeriod(_testCard.Id, 2025, 6, null);

        var request = new UpdateCardTransactionRequest
        {
            Description = "Compra Atualizada",
            Amount = 150m,
            Date = DateTime.UtcNow,
            Installment = "1/1",
            InvoiceYear = 2025,
            InvoiceMonth = 6,
            Type = CardTransactionType.Expense
        };

        // Act
        var result = await _service.UpdateCardTransactionAsync(transaction.Id, request);

        // Assert
        result.Should().NotBeNull();
        result!.Description.Should().Be("Compra Atualizada");
        result.Amount.Should().Be(150m);
    }

    [Fact]
    public async Task UpdateCardTransactionAsync_WhenTransactionNotFound_ShouldReturnNull()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        _mocks.CardTransactionRepository
            .Setup(r => r.GetByIdAsync(nonExistentId))
            .ReturnsAsync((CardTransaction?)null);

        var request = new UpdateCardTransactionRequest
        {
            Description = "Teste",
            Amount = 100m,
            Date = DateTime.UtcNow,
            Installment = "1/1",
            InvoiceYear = 2025,
            InvoiceMonth = 6,
            Type = CardTransactionType.Expense
        };

        // Act
        var result = await _service.UpdateCardTransactionAsync(nonExistentId, request);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region DeleteCardTransactionAsync Tests

    [Fact]
    public async Task DeleteCardTransactionAsync_WhenTransactionExists_ShouldDeleteAndUpdateLimit()
    {
        // Arrange
        var transaction = new CardTransaction(_testCard.Id, "Compra", 500m, DateTime.UtcNow, "1/1", 2025, 6, CardTransactionType.Expense);
        _mocks.SetupCardTransactionById(transaction);
        
        var existingInvoice = new FinancialTransaction(
            "Fatura Nubank", 500m, DateTime.UtcNow, TransactionType.PayableBill, _testCategory.Id, cardId: _testCard.Id);
        _mocks.SetupInvoiceByCardAndPeriod(_testCard.Id, 2025, 6, existingInvoice);

        var initialLimit = _testCard.CurrentLimit;

        // Act
        var result = await _service.DeleteCardTransactionAsync(transaction.Id);

        // Assert
        result.Should().BeTrue();
        _testCard.CurrentLimit.Should().Be(initialLimit + 500m); // Limite deve aumentar após deletar despesa
        _mocks.CardTransactionRepository.Verify(r => r.DeleteAsync(transaction.Id), Times.Once);
    }

    [Fact]
    public async Task DeleteCardTransactionAsync_WhenTransactionNotFound_ShouldReturnFalse()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        _mocks.CardTransactionRepository
            .Setup(r => r.GetByIdAsync(nonExistentId))
            .ReturnsAsync((CardTransaction?)null);

        // Act
        var result = await _service.DeleteCardTransactionAsync(nonExistentId);

        // Assert
        result.Should().BeFalse();
        _mocks.CardTransactionRepository.Verify(r => r.DeleteAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task DeleteCardTransactionAsync_ShouldDeleteInvoiceWhenAmountBecomesZero()
    {
        // Arrange
        var transaction = new CardTransaction(_testCard.Id, "Compra", 500m, DateTime.UtcNow, "1/1", 2025, 6, CardTransactionType.Expense);
        _mocks.SetupCardTransactionById(transaction);
        
        var existingInvoice = new FinancialTransaction(
            "Fatura Nubank", 500m, DateTime.UtcNow, TransactionType.PayableBill, _testCategory.Id, cardId: _testCard.Id);
        _mocks.SetupInvoiceByCardAndPeriod(_testCard.Id, 2025, 6, existingInvoice);

        // Act
        await _service.DeleteCardTransactionAsync(transaction.Id);

        // Assert
        _mocks.FinancialTransactionRepository.Verify(r => r.DeleteAsync(existingInvoice.Id), Times.Once);
    }

    #endregion
}

