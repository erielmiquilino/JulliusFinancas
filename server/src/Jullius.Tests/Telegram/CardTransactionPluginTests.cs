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
/// Testes para o CardTransactionPlugin (CreateCardPurchase, ListCards, GetCardInvoice, CalculateInvoicePeriod).
/// </summary>
public class CardTransactionPluginTests
{
    private readonly Mock<ICardRepository> _cardRepoMock = new();
    private readonly Mock<ICardTransactionRepository> _cardTransactionRepoMock = new();
    private readonly Mock<IFinancialTransactionRepository> _financialTransactionRepoMock = new();
    private readonly Mock<ICategoryRepository> _categoryRepoMock = new();
    private readonly CardTransactionPlugin _plugin;

    public CardTransactionPluginTests()
    {
        _cardTransactionRepoMock
            .Setup(r => r.CreateAsync(It.IsAny<CardTransaction>()))
            .ReturnsAsync((CardTransaction ct) => ct);

        _cardRepoMock
            .Setup(r => r.UpdateAsync(It.IsAny<Card>()))
            .Returns(Task.CompletedTask);

        _financialTransactionRepoMock
            .Setup(r => r.GetByCardIdAndPeriodAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync((FinancialTransaction?)null);

        _categoryRepoMock
            .Setup(r => r.GetOrCreateSystemCategoryAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new Category("Fatura Cartão", "#FF5722"));

        _financialTransactionRepoMock
            .Setup(r => r.CreateAsync(It.IsAny<FinancialTransaction>()))
            .ReturnsAsync((FinancialTransaction ft) => ft);

        var cardTransactionService = new CardTransactionService(
            _cardTransactionRepoMock.Object,
            _cardRepoMock.Object,
            _financialTransactionRepoMock.Object,
            _categoryRepoMock.Object);

        _plugin = new CardTransactionPlugin(
            cardTransactionService,
            _cardRepoMock.Object,
            Mock.Of<ILogger<CardTransactionPlugin>>());
    }

    #region ListCards

    [Fact]
    public async Task ListCards_ShouldReturnCardList()
    {
        var card1 = new Card("Nubank", "Nu Pagamentos", 3, 10, 5000m);
        card1.SetCurrentLimit(3500m);
        var card2 = new Card("Inter", "Banco Inter", 5, 15, 3000m);
        card2.SetCurrentLimit(2000m);
        var cards = new List<Card> { card1, card2 };
        _cardRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(cards);

        var result = await _plugin.ListCardsAsync();

        Assert.Contains("Nubank", result);
        Assert.Contains("Inter", result);
        Assert.Contains("5.000,00", result);
    }

    [Fact]
    public async Task ListCards_ShouldReturnEmpty_WhenNoCards()
    {
        _cardRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(Array.Empty<Card>());

        var result = await _plugin.ListCardsAsync();

        Assert.Contains("Nenhum cartão", result);
    }

    #endregion

    #region CreateCardPurchase

    [Fact]
    public async Task CreateCardPurchase_ShouldMatchCardByName()
    {
        var card = new Card("Nubank", "Nu Pagamentos", 3, 10, 5000m);
        card.SetCurrentLimit(4500m);
        _cardRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new[] { card });
        _cardRepoMock.Setup(r => r.GetByIdAsync(card.Id)).ReturnsAsync(card);

        var result = await _plugin.CreateCardPurchaseAsync("Tênis Nike", 350m, "nubank", 1);

        Assert.Contains("✅ Compra registrada", result);
        Assert.Contains("Tênis Nike", result);
        Assert.Contains("à vista", result);
    }

    [Fact]
    public async Task CreateCardPurchase_WithInstallments_ShouldShowPerInstallmentValue()
    {
        var card = new Card("Nubank", "Nu Pagamentos", 3, 10, 5000m);
        card.SetCurrentLimit(4000m);
        _cardRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new[] { card });
        _cardRepoMock.Setup(r => r.GetByIdAsync(card.Id)).ReturnsAsync(card);

        var result = await _plugin.CreateCardPurchaseAsync("iPhone", 6000m, "nubank", 12);

        Assert.Contains("12x de", result);
        Assert.Contains("500,00", result); // 6000 / 12
    }

    [Fact]
    public async Task CreateCardPurchase_ShouldReturnError_WhenCardNotFound()
    {
        _cardRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(Array.Empty<Card>());

        var result = await _plugin.CreateCardPurchaseAsync("Test", 100m, "Inexistente", 1);

        Assert.Contains("Nenhum cartão", result);
    }

    [Fact]
    public async Task CreateCardPurchase_ShouldSuggestCards_WhenNoMatch()
    {
        var card = new Card("Inter", "Banco Inter", 5, 15, 3000m);
        card.SetCurrentLimit(2500m);
        _cardRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new[] { card });

        var result = await _plugin.CreateCardPurchaseAsync("Test", 100m, "visa gold", 1);

        Assert.Contains("Inter", result);
        Assert.Contains("Qual deseja usar?", result);
    }

    #endregion

    #region CalculateInvoicePeriod

    [Fact]
    public void CalculateInvoicePeriod_TransactionBeforeClosing_ShouldBeCurrentMonth()
    {
        // Transaction on Jan 5, closing day 10, due day 15
        var date = new DateTime(2025, 1, 5, 12, 0, 0, DateTimeKind.Utc);
        var (year, month) = CardTransactionPlugin.CalculateInvoicePeriod(date, 10, 15);

        // Closing Jan 10 → due Jan 15  
        Assert.Equal(2025, year);
        Assert.Equal(1, month);
    }

    [Fact]
    public void CalculateInvoicePeriod_TransactionAfterClosing_ShouldBeNextMonth()
    {
        // Transaction on Jan 15, closing day 10, due day 15
        var date = new DateTime(2025, 1, 15, 12, 0, 0, DateTimeKind.Utc);
        var (year, month) = CardTransactionPlugin.CalculateInvoicePeriod(date, 10, 15);

        // Closing moves to Feb 10 → due Feb 15
        Assert.Equal(2025, year);
        Assert.Equal(2, month);
    }

    [Fact]
    public void CalculateInvoicePeriod_DueDayBeforeClosingDay_ShouldBeNextMonthDue()
    {
        // Transaction on Jan 5, closing day 25, due day 10 (next month)
        var date = new DateTime(2025, 1, 5, 12, 0, 0, DateTimeKind.Utc);
        var (year, month) = CardTransactionPlugin.CalculateInvoicePeriod(date, 25, 10);

        // Closing Jan 25 → next month for due → Feb 10 
        Assert.Equal(2025, year);
        Assert.Equal(2, month);
    }

    [Fact]
    public void CalculateInvoicePeriod_YearTransition_ShouldHandleDecember()
    {
        // Transaction on Dec 26, closing day 25, due day 15
        var date = new DateTime(2025, 12, 26, 12, 0, 0, DateTimeKind.Utc);
        var (year, month) = CardTransactionPlugin.CalculateInvoicePeriod(date, 25, 15);

        // Closing moves to Jan 25, 2026 → due day (15) < closing day (25) → next month → Feb 15, 2026
        Assert.Equal(2026, year);
        Assert.Equal(2, month);
    }

    #endregion

    #region GetCardInvoice

    [Fact]
    public async Task GetCardInvoice_ShouldReturnTransactions()
    {
        var card = new Card("Nubank", "Nu Pagamentos", 3, 10, 5000m);
        card.SetCurrentLimit(4000m);
        _cardRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new[] { card });
        _cardRepoMock.Setup(r => r.GetByIdAsync(card.Id)).ReturnsAsync(card);

        _cardTransactionRepoMock
            .Setup(r => r.GetByCardIdAndPeriodAsync(card.Id, 1, 2025))
            .ReturnsAsync(new List<CardTransaction>());

        var result = await _plugin.GetCardInvoiceAsync("nubank", 1, 2025);

        // No transactions for the period → should say "Nenhuma"
        Assert.Contains("Nenhuma", result);
    }

    [Fact]
    public async Task GetCardInvoice_ShouldReturnError_WhenCardNotFound()
    {
        _cardRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(Array.Empty<Card>());

        var result = await _plugin.GetCardInvoiceAsync("inexistente", 1, 2025);

        Assert.Contains("❌", result);
        Assert.Contains("não encontrado", result);
    }

    #endregion
}
