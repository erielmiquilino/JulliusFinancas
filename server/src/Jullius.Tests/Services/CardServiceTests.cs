using FluentAssertions;
using Jullius.Domain.Domain.Entities;
using Jullius.ServiceApi.Application.DTOs;
using Jullius.ServiceApi.Application.Services;
using Jullius.Tests.Mocks;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Jullius.Tests.Services;

public class CardServiceTests
{
    private readonly RepositoryMocks _mocks;
    private readonly Mock<ILogger<CardService>> _loggerMock;
    private readonly CardService _service;

    public CardServiceTests()
    {
        _mocks = new RepositoryMocks();
        _loggerMock = new Mock<ILogger<CardService>>();
        _service = new CardService(
            _mocks.CardRepository.Object,
            _mocks.FinancialTransactionRepository.Object,
            _mocks.CardTransactionRepository.Object,
            _loggerMock.Object
        );
    }

    #region CreateCardAsync Tests

    [Fact]
    public async Task CreateCardAsync_WithValidRequest_ShouldCreateCard()
    {
        // Arrange
        var request = new CreateCardRequest
        {
            Name = "Nubank",
            IssuingBank = "Nubank",
            ClosingDay = 15,
            DueDay = 22,
            Limit = 5000m
        };

        // Act
        var result = await _service.CreateCardAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be(request.Name);
        result.IssuingBank.Should().Be(request.IssuingBank);
        result.ClosingDay.Should().Be(request.ClosingDay);
        result.DueDay.Should().Be(request.DueDay);
        result.Limit.Should().Be(request.Limit);
        result.CurrentLimit.Should().Be(request.Limit);

        _mocks.CardRepository.Verify(r => r.CreateAsync(It.IsAny<Card>()), Times.Once);
    }

    [Theory]
    [InlineData("", "Nubank")]
    [InlineData(" ", "Nubank")]
    [InlineData(null, "Nubank")]
    public async Task CreateCardAsync_WithEmptyName_ShouldThrowArgumentException(string? name, string bank)
    {
        // Arrange
        var request = new CreateCardRequest
        {
            Name = name!,
            IssuingBank = bank,
            ClosingDay = 15,
            DueDay = 22,
            Limit = 5000m
        };

        // Act
        var act = async () => await _service.CreateCardAsync(request);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Nome do cartão é obrigatório");
    }

    [Theory]
    [InlineData("Nubank", "")]
    [InlineData("Nubank", " ")]
    [InlineData("Nubank", null)]
    public async Task CreateCardAsync_WithEmptyIssuingBank_ShouldThrowArgumentException(string name, string? bank)
    {
        // Arrange
        var request = new CreateCardRequest
        {
            Name = name,
            IssuingBank = bank!,
            ClosingDay = 15,
            DueDay = 22,
            Limit = 5000m
        };

        // Act
        var act = async () => await _service.CreateCardAsync(request);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Banco emissor é obrigatório");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(32)]
    public async Task CreateCardAsync_WithInvalidClosingDay_ShouldThrowArgumentException(int closingDay)
    {
        // Arrange
        var request = new CreateCardRequest
        {
            Name = "Nubank",
            IssuingBank = "Nubank",
            ClosingDay = closingDay,
            DueDay = 22,
            Limit = 5000m
        };

        // Act
        var act = async () => await _service.CreateCardAsync(request);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Dia de fechamento deve estar entre 1 e 31");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(32)]
    public async Task CreateCardAsync_WithInvalidDueDay_ShouldThrowArgumentException(int dueDay)
    {
        // Arrange
        var request = new CreateCardRequest
        {
            Name = "Nubank",
            IssuingBank = "Nubank",
            ClosingDay = 15,
            DueDay = dueDay,
            Limit = 5000m
        };

        // Act
        var act = async () => await _service.CreateCardAsync(request);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Dia de vencimento deve estar entre 1 e 31");
    }

    #endregion

    #region GetCardByIdAsync Tests

    [Fact]
    public async Task GetCardByIdAsync_WhenCardExists_ShouldReturnCard()
    {
        // Arrange
        var card = new Card("Nubank", "Nubank", 15, 22, 5000m);
        _mocks.SetupCardById(card);

        // Act
        var result = await _service.GetCardByIdAsync(card.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(card.Id);
    }

    [Fact]
    public async Task GetCardByIdAsync_WhenCardDoesNotExist_ShouldReturnNull()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        _mocks.CardRepository
            .Setup(r => r.GetByIdAsync(nonExistentId))
            .ReturnsAsync((Card?)null);

        // Act
        var result = await _service.GetCardByIdAsync(nonExistentId);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetAllCardsAsync Tests

    [Fact]
    public async Task GetAllCardsAsync_ShouldReturnAllCards()
    {
        // Arrange
        var cards = new List<Card>
        {
            new Card("Nubank", "Nubank", 15, 22, 5000m),
            new Card("Itaú", "Itaú", 10, 17, 10000m)
        };
        _mocks.SetupAllCards(cards);

        // Act
        var result = await _service.GetAllCardsAsync();

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAllCardsAsync_WhenNoCards_ShouldReturnEmptyList()
    {
        // Arrange
        _mocks.SetupAllCards(new List<Card>());

        // Act
        var result = await _service.GetAllCardsAsync();

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region UpdateCardAsync Tests

    [Fact]
    public async Task UpdateCardAsync_WithValidData_ShouldUpdateCard()
    {
        // Arrange
        var card = new Card("Nubank", "Nubank", 15, 22, 5000m);
        _mocks.SetupCardById(card);
        _mocks.SetupCardTransactionsFromPeriod(card.Id, It.IsAny<int>(), It.IsAny<int>(), new List<CardTransaction>());

        var request = new UpdateCardRequest
        {
            Name = "Nubank Atualizado",
            IssuingBank = "Nu Pagamentos",
            ClosingDay = 10,
            DueDay = 17,
            Limit = 7000m
        };

        // Act
        var result = await _service.UpdateCardAsync(card.Id, request);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be(request.Name);
        result.IssuingBank.Should().Be(request.IssuingBank);
        result.ClosingDay.Should().Be(request.ClosingDay);
        result.DueDay.Should().Be(request.DueDay);
        result.Limit.Should().Be(request.Limit);

        _mocks.CardRepository.Verify(r => r.UpdateAsync(It.IsAny<Card>()), Times.Once);
    }

    [Fact]
    public async Task UpdateCardAsync_WhenCardDoesNotExist_ShouldReturnNull()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        _mocks.CardRepository
            .Setup(r => r.GetByIdAsync(nonExistentId))
            .ReturnsAsync((Card?)null);

        var request = new UpdateCardRequest
        {
            Name = "Teste",
            IssuingBank = "Teste",
            ClosingDay = 15,
            DueDay = 22,
            Limit = 5000m
        };

        // Act
        var result = await _service.UpdateCardAsync(nonExistentId, request);

        // Assert
        result.Should().BeNull();
        _mocks.CardRepository.Verify(r => r.UpdateAsync(It.IsAny<Card>()), Times.Never);
    }

    [Fact]
    public async Task UpdateCardAsync_WhenLimitChanges_ShouldRecalculateCurrentLimit()
    {
        // Arrange
        var card = new Card("Nubank", "Nubank", 15, 22, 5000m);
        _mocks.SetupCardById(card);

        // Simula transações existentes que consomem parte do limite
        var transactions = new List<CardTransaction>
        {
            new CardTransaction(card.Id, "Compra 1", 1000m, DateTime.UtcNow, "1/1", 2025, 6, CardTransactionType.Expense),
            new CardTransaction(card.Id, "Compra 2", 500m, DateTime.UtcNow, "1/1", 2025, 6, CardTransactionType.Expense)
        };
        
        _mocks.CardTransactionRepository
            .Setup(r => r.GetByCardIdFromPeriodAsync(card.Id, It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(transactions);

        var request = new UpdateCardRequest
        {
            Name = "Nubank",
            IssuingBank = "Nubank",
            ClosingDay = 15,
            DueDay = 22,
            Limit = 10000m // Limite novo
        };

        // Act
        var result = await _service.UpdateCardAsync(card.Id, request);

        // Assert
        result.Should().NotBeNull();
        result!.Limit.Should().Be(10000m);
        // CurrentLimit = NovoLimite - (Despesas - Receitas) = 10000 - 1500 = 8500
        result.CurrentLimit.Should().Be(8500m);
    }

    #endregion

    #region DeleteCardAsync Tests

    [Fact]
    public async Task DeleteCardAsync_WhenCardExists_ShouldDeleteCardAndInvoices()
    {
        // Arrange
        var card = new Card("Nubank", "Nubank", 15, 22, 5000m);
        _mocks.SetupCardById(card);

        var invoices = new List<FinancialTransaction>
        {
            new FinancialTransaction("Fatura 1", 500m, DateTime.UtcNow, TransactionType.PayableBill, Guid.NewGuid(), cardId: card.Id),
            new FinancialTransaction("Fatura 2", 300m, DateTime.UtcNow, TransactionType.PayableBill, Guid.NewGuid(), cardId: card.Id)
        };
        _mocks.SetupFinancialTransactionsByCardId(card.Id, invoices);

        // Act
        var result = await _service.DeleteCardAsync(card.Id);

        // Assert
        result.Should().BeTrue();
        _mocks.FinancialTransactionRepository.Verify(r => r.DeleteAsync(It.IsAny<Guid>()), Times.Exactly(2));
        _mocks.CardRepository.Verify(r => r.DeleteAsync(card.Id), Times.Once);
    }

    [Fact]
    public async Task DeleteCardAsync_WhenCardDoesNotExist_ShouldReturnFalse()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        _mocks.CardRepository
            .Setup(r => r.GetByIdAsync(nonExistentId))
            .ReturnsAsync((Card?)null);

        // Act
        var result = await _service.DeleteCardAsync(nonExistentId);

        // Assert
        result.Should().BeFalse();
        _mocks.CardRepository.Verify(r => r.DeleteAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task DeleteCardAsync_WithNoInvoices_ShouldOnlyDeleteCard()
    {
        // Arrange
        var card = new Card("Nubank", "Nubank", 15, 22, 5000m);
        _mocks.SetupCardById(card);
        _mocks.SetupFinancialTransactionsByCardId(card.Id, new List<FinancialTransaction>());

        // Act
        var result = await _service.DeleteCardAsync(card.Id);

        // Assert
        result.Should().BeTrue();
        _mocks.FinancialTransactionRepository.Verify(r => r.DeleteAsync(It.IsAny<Guid>()), Times.Never);
        _mocks.CardRepository.Verify(r => r.DeleteAsync(card.Id), Times.Once);
    }

    #endregion
}

