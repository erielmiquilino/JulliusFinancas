using FluentAssertions;
using Jullius.Domain.Domain.Entities;
using Jullius.ServiceApi.Application.DTOs;
using Jullius.ServiceApi.Application.Services;
using Jullius.Tests.Mocks;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Jullius.Tests.Services;

public class FinancialTransactionServiceTests
{
    private readonly RepositoryMocks _mocks;
    private readonly Mock<IServiceProvider> _serviceProviderMock;
    private readonly FinancialTransactionService _service;
    private readonly Category _testCategory;
    private readonly Card _testCard;

    public FinancialTransactionServiceTests()
    {
        _mocks = new RepositoryMocks();
        _serviceProviderMock = new Mock<IServiceProvider>();
        
        _service = new FinancialTransactionService(
            _mocks.FinancialTransactionRepository.Object,
            _mocks.CardRepository.Object,
            _mocks.CategoryRepository.Object,
            _serviceProviderMock.Object
        );

        // Setup dados de teste comuns
        _testCategory = new Category("Alimentação", "#FF5722");
        _testCard = new Card("Nubank", "Nubank", 15, 22, 5000m);

        _mocks.SetupCategoryById(_testCategory);
        _mocks.SetupCardById(_testCard);
    }

    #region CreateTransactionAsync Tests

    [Fact]
    public async Task CreateTransactionAsync_WithSingleTransaction_ShouldCreateOneTransaction()
    {
        // Arrange
        var request = new CreateFinancialTransactionRequest
        {
            Description = "Conta de Luz",
            Amount = 250m,
            DueDate = DateTime.UtcNow.AddDays(30),
            Type = TransactionType.PayableBill,
            CategoryId = _testCategory.Id,
            IsPaid = false,
            IsInstallment = false
        };

        // Act
        var result = await _service.CreateTransactionAsync(request);

        // Assert
        result.Should().HaveCount(1);
        var transaction = result.First();
        transaction.Description.Should().Be("Conta de Luz");
        transaction.Amount.Should().Be(250m);
        transaction.Type.Should().Be(TransactionType.PayableBill);
        transaction.IsPaid.Should().BeFalse();

        _mocks.FinancialTransactionRepository.Verify(r => r.CreateAsync(It.IsAny<FinancialTransaction>()), Times.Once);
    }

    [Fact]
    public async Task CreateTransactionAsync_WithInstallments_ShouldCreateMultipleTransactions()
    {
        // Arrange
        var request = new CreateFinancialTransactionRequest
        {
            Description = "Compra Parcelada",
            Amount = 600m,
            DueDate = new DateTime(2025, 1, 15),
            Type = TransactionType.PayableBill,
            CategoryId = _testCategory.Id,
            IsPaid = false,
            IsInstallment = true,
            InstallmentCount = 3
        };

        // Act
        var result = await _service.CreateTransactionAsync(request);

        // Assert
        result.Should().HaveCount(3);
        
        var transactions = result.ToList();
        transactions[0].Description.Should().Be("Compra Parcelada (01/03)");
        transactions[1].Description.Should().Be("Compra Parcelada (02/03)");
        transactions[2].Description.Should().Be("Compra Parcelada (03/03)");

        _mocks.FinancialTransactionRepository.Verify(r => r.CreateAsync(It.IsAny<FinancialTransaction>()), Times.Exactly(3));
    }

    [Fact]
    public async Task CreateTransactionAsync_WithInstallments_ShouldDivideAmountCorrectly()
    {
        // Arrange
        var request = new CreateFinancialTransactionRequest
        {
            Description = "Compra",
            Amount = 100m,
            DueDate = DateTime.UtcNow,
            Type = TransactionType.PayableBill,
            CategoryId = _testCategory.Id,
            IsInstallment = true,
            InstallmentCount = 3
        };

        // Act
        var result = await _service.CreateTransactionAsync(request);

        // Assert
        var transactions = result.ToList();
        // 100 / 3 = 33.33, última parcela = 100 - (33.33 * 2) = 33.34
        transactions[0].Amount.Should().Be(33.33m);
        transactions[1].Amount.Should().Be(33.33m);
        transactions[2].Amount.Should().Be(33.34m);

        transactions.Sum(t => t.Amount).Should().Be(100m);
    }

    [Fact]
    public async Task CreateTransactionAsync_WithInstallments_ShouldIncrementDueDate()
    {
        // Arrange
        var startDate = new DateTime(2025, 1, 15);
        var request = new CreateFinancialTransactionRequest
        {
            Description = "Compra",
            Amount = 300m,
            DueDate = startDate,
            Type = TransactionType.PayableBill,
            CategoryId = _testCategory.Id,
            IsInstallment = true,
            InstallmentCount = 3
        };

        // Act
        var result = await _service.CreateTransactionAsync(request);

        // Assert
        var transactions = result.ToList();
        transactions[0].DueDate.Should().Be(new DateTime(2025, 1, 15));
        transactions[1].DueDate.Should().Be(new DateTime(2025, 2, 15));
        transactions[2].DueDate.Should().Be(new DateTime(2025, 3, 15));
    }

    #endregion

    #region GetAllTransactionsAsync Tests

    [Fact]
    public async Task GetAllTransactionsAsync_ShouldReturnAllTransactions()
    {
        // Arrange
        var transactions = new List<FinancialTransaction>
        {
            new FinancialTransaction("Conta 1", 100m, DateTime.UtcNow, TransactionType.PayableBill, _testCategory.Id),
            new FinancialTransaction("Conta 2", 200m, DateTime.UtcNow, TransactionType.PayableBill, _testCategory.Id)
        };
        
        _mocks.FinancialTransactionRepository
            .Setup(r => r.GetAllAsync())
            .ReturnsAsync(transactions);

        // Act
        var result = await _service.GetAllTransactionsAsync();

        // Assert
        result.Should().HaveCount(2);
    }

    #endregion

    #region UpdateTransactionAsync Tests

    [Fact]
    public async Task UpdateTransactionAsync_WithValidData_ShouldUpdateTransaction()
    {
        // Arrange
        var transaction = new FinancialTransaction("Conta Original", 100m, DateTime.UtcNow, TransactionType.PayableBill, _testCategory.Id);
        _mocks.SetupFinancialTransactionById(transaction);

        var request = new UpdateFinancialTransactionRequest
        {
            Description = "Conta Atualizada",
            Amount = 150m,
            DueDate = DateTime.UtcNow.AddDays(15),
            Type = TransactionType.ReceivableBill,
            CategoryId = _testCategory.Id,
            IsPaid = true
        };

        // Act
        var result = await _service.UpdateTransactionAsync(transaction.Id, request);

        // Assert
        result.Should().NotBeNull();
        result!.Description.Should().Be("Conta Atualizada");
        result.Amount.Should().Be(150m);
        result.Type.Should().Be(TransactionType.ReceivableBill);
        result.IsPaid.Should().BeTrue();

        _mocks.FinancialTransactionRepository.Verify(r => r.UpdateAsync(transaction), Times.Once);
    }

    [Fact]
    public async Task UpdateTransactionAsync_WhenNotFound_ShouldReturnNull()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        _mocks.FinancialTransactionRepository
            .Setup(r => r.GetByIdAsync(nonExistentId))
            .ReturnsAsync((FinancialTransaction?)null);

        var request = new UpdateFinancialTransactionRequest
        {
            Description = "Teste",
            Amount = 100m,
            DueDate = DateTime.UtcNow,
            Type = TransactionType.PayableBill,
            CategoryId = _testCategory.Id
        };

        // Act
        var result = await _service.UpdateTransactionAsync(nonExistentId, request);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region UpdatePaymentStatusAsync Tests

    [Fact]
    public async Task UpdatePaymentStatusAsync_ToTrue_ShouldMarkAsPaid()
    {
        // Arrange
        var transaction = new FinancialTransaction("Conta", 100m, DateTime.UtcNow, TransactionType.PayableBill, _testCategory.Id);
        _mocks.SetupFinancialTransactionById(transaction);

        // Act
        var result = await _service.UpdatePaymentStatusAsync(transaction.Id, true);

        // Assert
        result.Should().NotBeNull();
        result!.IsPaid.Should().BeTrue();
        _mocks.FinancialTransactionRepository.Verify(r => r.UpdateAsync(transaction), Times.Once);
    }

    [Fact]
    public async Task UpdatePaymentStatusAsync_ToFalse_ShouldMarkAsUnpaid()
    {
        // Arrange
        var transaction = new FinancialTransaction("Conta", 100m, DateTime.UtcNow, TransactionType.PayableBill, _testCategory.Id, isPaid: true);
        _mocks.SetupFinancialTransactionById(transaction);

        // Act
        var result = await _service.UpdatePaymentStatusAsync(transaction.Id, false);

        // Assert
        result.Should().NotBeNull();
        result!.IsPaid.Should().BeFalse();
    }

    [Fact]
    public async Task UpdatePaymentStatusAsync_WhenNotFound_ShouldReturnNull()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        _mocks.FinancialTransactionRepository
            .Setup(r => r.GetByIdAsync(nonExistentId))
            .ReturnsAsync((FinancialTransaction?)null);

        // Act
        var result = await _service.UpdatePaymentStatusAsync(nonExistentId, true);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdatePaymentStatusAsync_WithCardId_WhenPaid_ShouldIncreaseCardLimit()
    {
        // Arrange
        var transaction = new FinancialTransaction(
            "Fatura", 500m, DateTime.UtcNow, TransactionType.PayableBill, _testCategory.Id, 
            isPaid: false, cardId: _testCard.Id);
        _mocks.SetupFinancialTransactionById(transaction);

        var initialLimit = _testCard.CurrentLimit;

        // Act
        await _service.UpdatePaymentStatusAsync(transaction.Id, true);

        // Assert
        _testCard.CurrentLimit.Should().Be(initialLimit + 500m);
        _mocks.CardRepository.Verify(r => r.UpdateAsync(_testCard), Times.Once);
    }

    [Fact]
    public async Task UpdatePaymentStatusAsync_WithCardId_WhenUnpaid_ShouldDecreaseCardLimit()
    {
        // Arrange
        var transaction = new FinancialTransaction(
            "Fatura", 500m, DateTime.UtcNow, TransactionType.PayableBill, _testCategory.Id, 
            isPaid: true, cardId: _testCard.Id);
        _mocks.SetupFinancialTransactionById(transaction);

        var initialLimit = _testCard.CurrentLimit;

        // Act
        await _service.UpdatePaymentStatusAsync(transaction.Id, false);

        // Assert
        _testCard.CurrentLimit.Should().Be(initialLimit - 500m);
        _mocks.CardRepository.Verify(r => r.UpdateAsync(_testCard), Times.Once);
    }

    [Fact]
    public async Task UpdatePaymentStatusAsync_WithoutCardId_ShouldNotUpdateCardLimit()
    {
        // Arrange
        var transaction = new FinancialTransaction(
            "Conta", 500m, DateTime.UtcNow, TransactionType.PayableBill, _testCategory.Id);
        _mocks.SetupFinancialTransactionById(transaction);

        // Act
        await _service.UpdatePaymentStatusAsync(transaction.Id, true);

        // Assert
        _mocks.CardRepository.Verify(r => r.UpdateAsync(It.IsAny<Card>()), Times.Never);
    }

    [Fact]
    public async Task UpdatePaymentStatusAsync_WhenStatusDoesNotChange_ShouldNotUpdateCardLimit()
    {
        // Arrange
        var transaction = new FinancialTransaction(
            "Fatura", 500m, DateTime.UtcNow, TransactionType.PayableBill, _testCategory.Id, 
            isPaid: true, cardId: _testCard.Id);
        _mocks.SetupFinancialTransactionById(transaction);

        // Act
        await _service.UpdatePaymentStatusAsync(transaction.Id, true); // Mesmo status

        // Assert
        _mocks.CardRepository.Verify(r => r.UpdateAsync(It.IsAny<Card>()), Times.Never);
    }

    #endregion

    #region DeleteTransactionAsync Tests

    [Fact]
    public async Task DeleteTransactionAsync_ShouldDeleteAndReturnTrue()
    {
        // Arrange
        var transactionId = Guid.NewGuid();

        // Act
        var result = await _service.DeleteTransactionAsync(transactionId);

        // Assert
        result.Should().BeTrue();
        _mocks.FinancialTransactionRepository.Verify(r => r.DeleteAsync(transactionId), Times.Once);
    }

    #endregion

    #region PayWithCardAsync Tests

    [Fact]
    public async Task PayWithCardAsync_WithEmptyTransactionIds_ShouldThrowArgumentException()
    {
        // Arrange
        var request = new PayWithCardRequest
        {
            TransactionIds = new List<Guid>(),
            CardId = _testCard.Id,
            CardAmount = 100m,
            InvoiceYear = 2025,
            InvoiceMonth = 6
        };

        // Act
        var act = async () => await _service.PayWithCardAsync(request);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Nenhuma transação foi selecionada");
    }

    [Fact]
    public async Task PayWithCardAsync_WithZeroCardAmount_ShouldThrowArgumentException()
    {
        // Arrange
        var transaction = new FinancialTransaction("Conta", 100m, DateTime.UtcNow, TransactionType.PayableBill, _testCategory.Id);
        
        var request = new PayWithCardRequest
        {
            TransactionIds = new List<Guid> { transaction.Id },
            CardId = _testCard.Id,
            CardAmount = 0m,
            InvoiceYear = 2025,
            InvoiceMonth = 6
        };

        // Act
        var act = async () => await _service.PayWithCardAsync(request);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("O valor do cartão deve ser maior que zero");
    }

    [Fact]
    public async Task PayWithCardAsync_WhenCardNotFound_ShouldThrowArgumentException()
    {
        // Arrange
        var nonExistentCardId = Guid.NewGuid();
        _mocks.CardRepository
            .Setup(r => r.GetByIdAsync(nonExistentCardId))
            .ReturnsAsync((Card?)null);

        var transaction = new FinancialTransaction("Conta", 100m, DateTime.UtcNow, TransactionType.PayableBill, _testCategory.Id);
        
        var request = new PayWithCardRequest
        {
            TransactionIds = new List<Guid> { transaction.Id },
            CardId = nonExistentCardId,
            CardAmount = 100m,
            InvoiceYear = 2025,
            InvoiceMonth = 6
        };

        // Act
        var act = async () => await _service.PayWithCardAsync(request);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Cartão não encontrado");
    }

    [Fact]
    public async Task PayWithCardAsync_WhenTransactionNotFound_ShouldThrowArgumentException()
    {
        // Arrange
        var nonExistentTransactionId = Guid.NewGuid();
        _mocks.FinancialTransactionRepository
            .Setup(r => r.GetByIdAsync(nonExistentTransactionId))
            .ReturnsAsync((FinancialTransaction?)null);

        var request = new PayWithCardRequest
        {
            TransactionIds = new List<Guid> { nonExistentTransactionId },
            CardId = _testCard.Id,
            CardAmount = 100m,
            InvoiceYear = 2025,
            InvoiceMonth = 6
        };

        // Act
        var act = async () => await _service.PayWithCardAsync(request);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage($"Transação {nonExistentTransactionId} não encontrada");
    }

    [Fact]
    public async Task PayWithCardAsync_ShouldMarkAllTransactionsAsPaid()
    {
        // Arrange
        var transaction1 = new FinancialTransaction("Conta 1", 100m, DateTime.UtcNow, TransactionType.PayableBill, _testCategory.Id);
        var transaction2 = new FinancialTransaction("Conta 2", 200m, DateTime.UtcNow, TransactionType.PayableBill, _testCategory.Id);
        
        _mocks.SetupFinancialTransactionById(transaction1);
        _mocks.FinancialTransactionRepository
            .Setup(r => r.GetByIdAsync(transaction2.Id))
            .ReturnsAsync(transaction2);

        _mocks.SetupSystemCategory(new Category("Cartão", "#9C27B0"));

        // Setup para o CardTransactionService que será resolvido via DI
        var cardTransactionService = new CardTransactionService(
            _mocks.CardTransactionRepository.Object,
            _mocks.CardRepository.Object,
            _mocks.FinancialTransactionRepository.Object,
            _mocks.CategoryRepository.Object
        );

        var serviceScopeMock = new Mock<IServiceScope>();
        var serviceScopeFactoryMock = new Mock<IServiceScopeFactory>();
        
        _serviceProviderMock
            .Setup(sp => sp.GetService(typeof(CardTransactionService)))
            .Returns(cardTransactionService);

        _mocks.SetupInvoiceByCardAndPeriod(_testCard.Id, 2025, 6, null);

        var request = new PayWithCardRequest
        {
            TransactionIds = new List<Guid> { transaction1.Id, transaction2.Id },
            CardId = _testCard.Id,
            CardAmount = 300m,
            InvoiceYear = 2025,
            InvoiceMonth = 6
        };

        // Act
        var result = await _service.PayWithCardAsync(request);

        // Assert
        result.PaidTransactionsCount.Should().Be(2);
        transaction1.IsPaid.Should().BeTrue();
        transaction2.IsPaid.Should().BeTrue();
        result.IncomeTransactionId.Should().NotBeEmpty();
        result.CardTransactionIds.Should().HaveCount(1);
    }

    #endregion
}

