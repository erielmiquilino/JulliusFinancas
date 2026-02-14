using FluentAssertions;
using Jullius.Domain.Domain.Entities;
using Jullius.Domain.Domain.Repositories;
using Jullius.ServiceApi.Application.DTOs;
using Jullius.ServiceApi.Application.Services;
using Jullius.ServiceApi.Telegram;
using Jullius.ServiceApi.Telegram.IntentHandlers;
using Jullius.Tests.Mocks;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Jullius.Tests.Telegram;

public class CreateCardPurchaseHandlerTests
{
    private readonly RepositoryMocks _mocks;
    private readonly Mock<IServiceProvider> _serviceProviderMock;
    private readonly CardTransactionService _cardTransactionService;
    private readonly CreateCardPurchaseHandler _handler;
    private readonly Card _testCard;

    public CreateCardPurchaseHandlerTests()
    {
        _mocks = new RepositoryMocks();
        _serviceProviderMock = new Mock<IServiceProvider>();
        _cardTransactionService = new CardTransactionService(
            _mocks.CardTransactionRepository.Object,
            _mocks.CardRepository.Object,
            _mocks.FinancialTransactionRepository.Object,
            _mocks.CategoryRepository.Object
        );

        _testCard = new Card("Nubank", "Nubank", 15, 22, 5000m);

        _handler = new CreateCardPurchaseHandler(
            _cardTransactionService,
            _mocks.CardRepository.Object,
            new Mock<ILogger<CreateCardPurchaseHandler>>().Object
        );
    }

    #region GetMissingFields

    [Fact]
    public void GetMissingFields_ShouldReturnAll_WhenStateEmpty()
    {
        var state = new ConversationState();

        var missing = _handler.GetMissingFields(state);

        missing.Should().HaveCount(3);
        missing.Should().Contain(["description", "amount", "cardName"]);
    }

    [Fact]
    public void GetMissingFields_ShouldReturnEmpty_WhenAllFieldsPresent()
    {
        var state = new ConversationState();
        state.SetData("description", "Compra");
        state.SetData("amount", 500m);
        state.SetData("cardName", "Nubank");

        var missing = _handler.GetMissingFields(state);

        missing.Should().BeEmpty();
    }

    [Fact]
    public void GetMissingFields_ShouldReturnPartial_WhenOnlyCardNameMissing()
    {
        var state = new ConversationState();
        state.SetData("description", "Notebook");
        state.SetData("amount", 3000m);

        var missing = _handler.GetMissingFields(state);

        missing.Should().ContainSingle().Which.Should().Be("cardName");
    }

    #endregion

    #region BuildConfirmationMessage

    [Fact]
    public void BuildConfirmationMessage_ShouldShowAVista_WhenNoInstallments()
    {
        var state = new ConversationState();
        state.SetData("description", "Compra");
        state.SetData("amount", 200m);
        state.SetData("cardName", "Nubank");

        var message = _handler.BuildConfirmationMessage(state);

        message.Should().Contain("À vista");
        message.Should().Contain("R$ 200,00");
        message.Should().Contain("Nubank");
    }

    [Fact]
    public void BuildConfirmationMessage_ShouldShowInstallments_WhenMultiple()
    {
        var state = new ConversationState();
        state.SetData("description", "Notebook");
        state.SetData("amount", 3000m);
        state.SetData("cardName", "Inter");
        state.SetData("installments", 10);

        var message = _handler.BuildConfirmationMessage(state);

        message.Should().Contain("10x de R$ 300,00");
        message.Should().Contain("R$ 3.000,00");
    }

    #endregion

    #region HandleAsync

    [Fact]
    public async Task HandleAsync_ShouldAskForMissingFields_WhenIncomplete()
    {
        var state = new ConversationState();
        state.SetData("description", "Compra");
        state.SetData("amount", 200m);
        // cardName missing

        _mocks.SetupAllCards(new[] { _testCard });

        var result = await _handler.HandleAsync(state);

        state.Phase.Should().Be(ConversationPhase.CollectingData);
        result.Should().Contain("cartão");
    }

    [Fact]
    public async Task HandleAsync_ShouldResolveCard_WhenComplete()
    {
        var state = new ConversationState();
        state.SetData("description", "Compra");
        state.SetData("amount", 200m);
        state.SetData("cardName", "Nubank");

        _mocks.SetupAllCards(new[] { _testCard });

        var result = await _handler.HandleAsync(state);

        state.Phase.Should().Be(ConversationPhase.AwaitingConfirmation);
        state.GetData<Guid>("cardId").Should().Be(_testCard.Id);
        result.Should().Contain("Confirma");
    }

    [Fact]
    public async Task HandleAsync_ShouldSuggestCards_WhenCardNotFound()
    {
        var state = new ConversationState();
        state.SetData("description", "Compra");
        state.SetData("amount", 200m);
        state.SetData("cardName", "CartãoInexistente");

        _mocks.SetupAllCards(new[] { _testCard });

        var result = await _handler.HandleAsync(state);

        result.Should().Contain("Não encontrei");
        result.Should().Contain("Nubank");
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnError_WhenNoCardsRegistered()
    {
        var state = new ConversationState();
        state.SetData("description", "Compra");
        state.SetData("amount", 200m);
        state.SetData("cardName", "Qualquer");

        _mocks.SetupAllCards(Array.Empty<Card>());

        var result = await _handler.HandleAsync(state);

        result.Should().Contain("Nenhum cartão cadastrado");
    }

    #endregion

    #region HandleConfirmationAsync

    [Fact]
    public async Task HandleConfirmationAsync_ShouldReturnCancelled_WhenNotConfirmed()
    {
        var state = new ConversationState();

        var result = await _handler.HandleConfirmationAsync(state, false);

        result.Should().Contain("cancelada");
    }

    [Fact]
    public async Task HandleConfirmationAsync_ShouldReturnError_WhenCardNotFound()
    {
        var state = new ConversationState();
        state.SetData("cardId", Guid.NewGuid());

        _mocks.CardRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync((Card?)null);

        var result = await _handler.HandleConfirmationAsync(state, true);

        result.Should().Contain("Cartão não encontrado");
    }

    [Fact]
    public async Task HandleConfirmationAsync_ShouldCreateTransaction_WhenConfirmed()
    {
        // Arrange
        _mocks.SetupCardById(_testCard);

        // Setup para buscar existing invoice e categorias necessárias
        var invoiceCategory = new Category("Fatura Cartão", "#2196F3");
        _mocks.CategoryRepository
            .Setup(r => r.GetOrCreateSystemCategoryAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(invoiceCategory);

        _mocks.FinancialTransactionRepository
            .Setup(r => r.GetByCardIdAndPeriodAsync(_testCard.Id, It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync((FinancialTransaction?)null);

        var state = new ConversationState();
        state.SetData("description", "Compra teste");
        state.SetData("amount", 200m);
        state.SetData("cardName", "Nubank");
        state.SetData("cardId", _testCard.Id);
        state.SetData("installments", 1);

        // Act
        var result = await _handler.HandleConfirmationAsync(state, true);

        // Assert
        result.Should().Contain("sucesso");
        result.Should().Contain("Compra teste");

        _mocks.CardTransactionRepository.Verify(
            r => r.CreateAsync(It.Is<CardTransaction>(ct =>
                ct.Description == "Compra teste" &&
                ct.Amount == 200m)),
            Times.Once);
    }

    #endregion
}
