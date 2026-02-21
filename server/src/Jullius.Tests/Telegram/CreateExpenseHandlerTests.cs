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

public class CreateExpenseHandlerTests
{
    private readonly RepositoryMocks _mocks;
    private readonly Mock<IServiceProvider> _serviceProviderMock;
    private readonly FinancialTransactionService _transactionService;
    private readonly CreateExpenseHandler _handler;
    private readonly Category _testCategory;

    public CreateExpenseHandlerTests()
    {
        _mocks = new RepositoryMocks();
        _serviceProviderMock = new Mock<IServiceProvider>();
        _transactionService = new FinancialTransactionService(
            _mocks.FinancialTransactionRepository.Object,
            _mocks.CardRepository.Object,
            _mocks.CategoryRepository.Object,
            _serviceProviderMock.Object
        );

        _testCategory = new Category("Essenciais", "#4CAF50");

        _handler = new CreateExpenseHandler(
            _transactionService,
            _mocks.CategoryRepository.Object,
            new Mock<ILogger<CreateExpenseHandler>>().Object
        );
    }

    #region GetMissingFields

    [Fact]
    public void GetMissingFields_ShouldReturnAll_WhenStateEmpty()
    {
        var state = new ConversationState();

        var missing = _handler.GetMissingFields(state);

        missing.Should().HaveCount(3);
        missing.Should().Contain(["description", "amount", "categoryName"]);
    }

    [Fact]
    public void GetMissingFields_ShouldReturnEmpty_WhenAllFieldsPresent()
    {
        var state = new ConversationState();
        state.SetData("description", "Almoço");
        state.SetData("amount", 25m);
        state.SetData("categoryName", "Alimentação");

        var missing = _handler.GetMissingFields(state);

        missing.Should().BeEmpty();
    }

    [Fact]
    public void GetMissingFields_ShouldReturnPartial_WhenSomeFieldsMissing()
    {
        var state = new ConversationState();
        state.SetData("description", "Almoço");

        var missing = _handler.GetMissingFields(state);

        missing.Should().HaveCount(2);
        missing.Should().Contain(["amount", "categoryName"]);
        missing.Should().NotContain("description");
    }

    #endregion

    #region BuildConfirmationMessage

    [Fact]
    public void BuildConfirmationMessage_ShouldIncludeAllData()
    {
        var state = new ConversationState();
        state.SetData("description", "Almoço");
        state.SetData("amount", 45.90m);
        state.SetData("categoryName", "Alimentação");

        var message = _handler.BuildConfirmationMessage(state);

        message.Should().Contain("Almoço");
        message.Should().Contain("45,90");
        message.Should().Contain("Alimentação");
        message.Should().Contain("Despesa");
    }

    [Fact]
    public void BuildConfirmationMessage_ShouldShowPago_WhenIsPaidTrue()
    {
        var state = new ConversationState();
        state.SetData("description", "Conta de Luz");
        state.SetData("amount", 120m);
        state.SetData("categoryName", "Essenciais");
        state.SetData("isPaid", true);

        var message = _handler.BuildConfirmationMessage(state);

        message.Should().Contain("✅ Pago");
        message.Should().NotContain("Pendente");
    }

    [Fact]
    public void BuildConfirmationMessage_ShouldShowPendente_WhenIsPaidFalse()
    {
        var state = new ConversationState();
        state.SetData("description", "Conta de Água");
        state.SetData("amount", 80m);
        state.SetData("categoryName", "Essenciais");
        state.SetData("isPaid", false);

        var message = _handler.BuildConfirmationMessage(state);

        message.Should().Contain("⏳ Pendente");
    }

    [Fact]
    public void BuildConfirmationMessage_ShouldShowPendente_WhenIsPaidNotSet()
    {
        var state = new ConversationState();
        state.SetData("description", "Internet");
        state.SetData("amount", 100m);
        state.SetData("categoryName", "Essenciais");

        var message = _handler.BuildConfirmationMessage(state);

        message.Should().Contain("⏳ Pendente");
    }

    #endregion

    #region HandleConfirmationAsync

    [Fact]
    public async Task HandleConfirmationAsync_ShouldReturnCancelled_WhenNotConfirmed()
    {
        var state = new ConversationState();

        var result = await _handler.HandleConfirmationAsync(state, false);

        result.Should().Contain("cancelado");
    }

    [Fact]
    public async Task HandleConfirmationAsync_ShouldCreateTransaction_WithIsPaidTrue()
    {
        // Arrange
        _mocks.CategoryRepository
            .Setup(r => r.GetByNameAsync("Essenciais"))
            .ReturnsAsync(_testCategory);

        var state = new ConversationState();
        state.SetData("description", "Almoço no Myata");
        state.SetData("amount", 22.50m);
        state.SetData("categoryName", "Essenciais");
        state.SetData("isPaid", true);

        // Act
        var result = await _handler.HandleConfirmationAsync(state, true);

        // Assert
        result.Should().Contain("sucesso");
        result.Should().Contain("Almoço no Myata");
        result.Should().Contain("✅");

        _mocks.FinancialTransactionRepository.Verify(
            r => r.CreateAsync(It.Is<FinancialTransaction>(t =>
                t.Description == "Almoço no Myata" &&
                t.Amount == 22.50m &&
                t.IsPaid == true)),
            Times.Once);
    }

    [Fact]
    public async Task HandleConfirmationAsync_ShouldCreateTransaction_WithIsPaidFalse_WhenNotSet()
    {
        // Arrange
        _mocks.CategoryRepository
            .Setup(r => r.GetByNameAsync("Alimentação"))
            .ReturnsAsync(_testCategory);

        var state = new ConversationState();
        state.SetData("description", "Café");
        state.SetData("amount", 10m);
        state.SetData("categoryName", "Alimentação");
        // isPaid NOT set

        // Act
        var result = await _handler.HandleConfirmationAsync(state, true);

        // Assert
        result.Should().Contain("sucesso");

        _mocks.FinancialTransactionRepository.Verify(
            r => r.CreateAsync(It.Is<FinancialTransaction>(t =>
                t.IsPaid == false)),
            Times.Once);
    }

    [Fact]
    public async Task HandleConfirmationAsync_ShouldAutoCreateCategory_WhenNotFound()
    {
        // Arrange
        var autoCategory = new Category("Nova Categoria", "#607D8B");

        _mocks.CategoryRepository
            .Setup(r => r.GetByNameAsync("Nova Categoria"))
            .ReturnsAsync((Category?)null);

        _mocks.CategoryRepository
            .Setup(r => r.GetOrCreateSystemCategoryAsync("Nova Categoria", "#607D8B"))
            .ReturnsAsync(autoCategory);

        var state = new ConversationState();
        state.SetData("description", "Teste");
        state.SetData("amount", 100m);
        state.SetData("categoryName", "Nova Categoria");

        // Act
        var result = await _handler.HandleConfirmationAsync(state, true);

        // Assert
        result.Should().Contain("sucesso");
        _mocks.CategoryRepository.Verify(
            r => r.GetOrCreateSystemCategoryAsync("Nova Categoria", "#607D8B"),
            Times.Once);
    }

    #endregion

    #region HandleAsync

    [Fact]
    public async Task HandleAsync_ShouldAskForMissingFields_WhenIncomplete()
    {
        var state = new ConversationState();
        state.SetData("description", "Almoço");
        // amount e categoryName missing

        var result = await _handler.HandleAsync(state);

        state.Phase.Should().Be(ConversationPhase.CollectingData);
        result.Should().Contain("Qual o valor");
    }

    [Fact]
    public async Task HandleAsync_ShouldBuildConfirmation_WhenComplete()
    {
        var state = new ConversationState();
        state.SetData("description", "Almoço");
        state.SetData("amount", 35m);
        state.SetData("categoryName", "Alimentação");

        var result = await _handler.HandleAsync(state);

        state.Phase.Should().Be(ConversationPhase.AwaitingConfirmation);
        result.Should().Contain("Confirma");
    }

    [Fact]
    public async Task HandleAsync_ShouldListExistingCategories_WhenCategoryMissing()
    {
        // Arrange: categories exist
        var categories = new List<Category>
        {
            new("Alimentação", "#4CAF50"),
            new("Saúde", "#2196F3"),
            new("Lazer", "#FF9800")
        };

        _mocks.CategoryRepository
            .Setup(r => r.GetAllAsync())
            .ReturnsAsync(categories);

        var state = new ConversationState();
        state.SetData("description", "Almoço");
        state.SetData("amount", 45m);
        // categoryName missing

        var result = await _handler.HandleAsync(state);

        state.Phase.Should().Be(ConversationPhase.CollectingData);
        result.Should().Contain("Alimentação");
        result.Should().Contain("Saúde");
        result.Should().Contain("Lazer");
        result.Should().Contain("Suas categorias");
    }

    [Fact]
    public async Task HandleAsync_ShouldShowGenericCategoryPrompt_WhenNoCategoriesExist()
    {
        // Arrange: no categories
        _mocks.CategoryRepository
            .Setup(r => r.GetAllAsync())
            .ReturnsAsync(Enumerable.Empty<Category>());

        var state = new ConversationState();
        state.SetData("description", "Almoço");
        state.SetData("amount", 45m);
        // categoryName missing

        var result = await _handler.HandleAsync(state);

        state.Phase.Should().Be(ConversationPhase.CollectingData);
        result.Should().Contain("categoria");
        result.Should().Contain("Alimentação");
        result.Should().Contain("Saúde");
        result.Should().Contain("Lazer");
    }

    #endregion
}
