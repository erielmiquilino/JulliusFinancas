using FluentAssertions;
using Jullius.Domain.Domain.Entities;
using Jullius.Domain.Domain.Repositories;
using Jullius.ServiceApi.Application.DTOs;
using Jullius.ServiceApi.Application.Services;
using Jullius.ServiceApi.Telegram;
using Jullius.ServiceApi.Telegram.IntentHandlers;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Jullius.Tests.Telegram;

public class ConversationOrchestratorTests
{
    private readonly ConversationStateStore _stateStore;
    private readonly Mock<GeminiAssistantService> _geminiServiceMock;
    private readonly Mock<IIntentHandler> _expenseHandlerMock;
    private readonly Mock<IIntentHandler> _cardPurchaseHandlerMock;
    private readonly Mock<IIntentHandler> _consultingHandlerMock;
    private readonly Mock<ICategoryRepository> _categoryRepositoryMock;
    private readonly ConversationOrchestrator _orchestrator;

    public ConversationOrchestratorTests()
    {
        _stateStore = new ConversationStateStore();

        // BotConfigurationService requires non-trivial constructor — mock its dependencies
        var dataProtectorMock = new Mock<IDataProtector>();
        var dataProtectionProviderMock = new Mock<IDataProtectionProvider>();
        dataProtectionProviderMock
            .Setup(p => p.CreateProtector(It.IsAny<string>()))
            .Returns(dataProtectorMock.Object);

        var botConfigService = new BotConfigurationService(
            new Mock<IBotConfigurationRepository>().Object,
            dataProtectionProviderMock.Object,
            new Mock<ILogger<BotConfigurationService>>().Object
        );

        _geminiServiceMock = new Mock<GeminiAssistantService>(
            botConfigService,
            Mock.Of<ILogger<GeminiAssistantService>>(),
            Mock.Of<IHttpClientFactory>()
        );

        _expenseHandlerMock = new Mock<IIntentHandler>();
        _expenseHandlerMock.Setup(h => h.HandledIntent).Returns(IntentType.CreateExpense);

        _cardPurchaseHandlerMock = new Mock<IIntentHandler>();
        _cardPurchaseHandlerMock.Setup(h => h.HandledIntent).Returns(IntentType.CreateCardPurchase);

        _consultingHandlerMock = new Mock<IIntentHandler>();
        _consultingHandlerMock.Setup(h => h.HandledIntent).Returns(IntentType.FinancialConsulting);

        _categoryRepositoryMock = new Mock<ICategoryRepository>();
        _categoryRepositoryMock
            .Setup(r => r.GetAllAsync())
            .ReturnsAsync(Enumerable.Empty<Category>());

        var handlers = new List<IIntentHandler>
        {
            _expenseHandlerMock.Object,
            _cardPurchaseHandlerMock.Object,
            _consultingHandlerMock.Object
        };

        _orchestrator = new ConversationOrchestrator(
            _stateStore,
            _geminiServiceMock.Object,
            handlers,
            _categoryRepositoryMock.Object,
            Mock.Of<ILogger<ConversationOrchestrator>>()
        );
    }

    private const long TestChatId = 12345L;

    #region Help / Cancel Commands

    [Theory]
    [InlineData("/start")]
    [InlineData("/ajuda")]
    [InlineData("/help")]
    public async Task ProcessMessage_ShouldReturnHelp_WhenHelpCommand(string command)
    {
        var result = await _orchestrator.ProcessMessageAsync(TestChatId, command);

        result.Should().Contain("Jullius Finanças");
        result.Should().Contain("Registrar despesa");
    }

    [Theory]
    [InlineData("/cancelar")]
    [InlineData("/cancel")]
    [InlineData("/reset")]
    public async Task ProcessMessage_ShouldReturnNothingToCancel_WhenIdleCancel(string command)
    {
        var result = await _orchestrator.ProcessMessageAsync(TestChatId, command);

        result.Should().Contain("Nada a cancelar");
    }

    #endregion

    #region Single Expense Flow

    [Fact]
    public async Task ProcessMessage_ShouldAskConfirmation_WhenSingleExpenseComplete()
    {
        // Arrange: Gemini retorna 1 tx com todos os dados
        var geminiResponse = new List<GeminiIntentResponse>
        {
            new()
            {
                Intent = "CREATE_EXPENSE",
                Confidence = 0.95,
                Data = new GeminiExtractedData
                {
                    Description = "Almoço",
                    Amount = 45m,
                    CategoryName = "Alimentação",
                    IsPaid = false
                }
            }
        };

        _geminiServiceMock
            .Setup(g => g.ClassifyIntentAsync(It.IsAny<string>(), It.IsAny<List<ChatMessage>>()))
            .ReturnsAsync(geminiResponse);

        // Handler retorna vazio para missing fields (tudo completo)
        _expenseHandlerMock
            .Setup(h => h.GetMissingFields(It.IsAny<ConversationState>()))
            .Returns(new List<string>());

        _expenseHandlerMock
            .Setup(h => h.BuildConfirmationMessage(It.IsAny<ConversationState>()))
            .Returns("Confirma o lançamento?");

        // Act
        var result = await _orchestrator.ProcessMessageAsync(TestChatId, "Gastei 45 de almoço em alimentação");

        // Assert
        result.Should().Contain("Confirma");
        var state = _stateStore.GetOrCreate(TestChatId);
        state.Phase.Should().Be(ConversationPhase.AwaitingConfirmation);
        state.PendingTransactions.Should().HaveCount(1);
    }

    [Fact]
    public async Task ProcessMessage_ShouldAskMissingFields_WhenSingleExpenseIncomplete()
    {
        // Arrange: Gemini retorna 1 tx com categoria faltando
        var geminiResponse = new List<GeminiIntentResponse>
        {
            new()
            {
                Intent = "CREATE_EXPENSE",
                Confidence = 0.95,
                Data = new GeminiExtractedData
                {
                    Description = "Almoço",
                    Amount = 45m,
                    CategoryName = null
                }
            }
        };

        _geminiServiceMock
            .Setup(g => g.ClassifyIntentAsync(It.IsAny<string>(), It.IsAny<List<ChatMessage>>()))
            .ReturnsAsync(geminiResponse);

        _expenseHandlerMock
            .Setup(h => h.GetMissingFields(It.IsAny<ConversationState>()))
            .Returns(new List<string> { "categoryName" });

        // Act
        var result = await _orchestrator.ProcessMessageAsync(TestChatId, "Gastei 45 de almoço");

        // Assert
        result.Should().Contain("Categoria");
        var state = _stateStore.GetOrCreate(TestChatId);
        state.Phase.Should().Be(ConversationPhase.CollectingData);
    }

    #endregion

    #region Multiple Transactions Flow (Batch)

    [Fact]
    public async Task ProcessMessage_ShouldBuildBatchConfirmation_WhenMultipleTransactionsComplete()
    {
        // Arrange: 2 despesas completas
        var geminiResponse = new List<GeminiIntentResponse>
        {
            new()
            {
                Intent = "CREATE_EXPENSE",
                Confidence = 0.95,
                Data = new GeminiExtractedData
                {
                    Description = "Almoço",
                    Amount = 22.50m,
                    CategoryName = "Essenciais",
                    IsPaid = true
                }
            },
            new()
            {
                Intent = "CREATE_EXPENSE",
                Confidence = 0.90,
                Data = new GeminiExtractedData
                {
                    Description = "Carregador Samsung",
                    Amount = 79m,
                    CategoryName = "Não planejado",
                    IsPaid = true
                }
            }
        };

        _geminiServiceMock
            .Setup(g => g.ClassifyIntentAsync(It.IsAny<string>(), It.IsAny<List<ChatMessage>>()))
            .ReturnsAsync(geminiResponse);

        // Ambos completos
        _expenseHandlerMock
            .Setup(h => h.GetMissingFields(It.IsAny<ConversationState>()))
            .Returns(new List<string>());

        // Act
        var result = await _orchestrator.ProcessMessageAsync(TestChatId,
            "Lance uma despesa de 22,50 no myata em essenciais E lance a despesa carregador Samsung 79 em não planejado as duas pagas");

        // Assert
        var state = _stateStore.GetOrCreate(TestChatId);
        state.Phase.Should().Be(ConversationPhase.AwaitingConfirmation);
        state.PendingTransactions.Should().HaveCount(2);
        result.Should().Contain("2 lançamentos");
        result.Should().Contain("Almoço");
        result.Should().Contain("Carregador Samsung");
        result.Should().Contain("✅ Pago");
    }

    [Fact]
    public async Task ProcessMessage_ShouldCollectMissing_WhenBatchHasIncompleteTransaction()
    {
        // Arrange: 2 despesas, segunda sem categoria
        var geminiResponse = new List<GeminiIntentResponse>
        {
            new()
            {
                Intent = "CREATE_EXPENSE",
                Confidence = 0.95,
                Data = new GeminiExtractedData
                {
                    Description = "Almoço",
                    Amount = 25m,
                    CategoryName = "Alimentação"
                }
            },
            new()
            {
                Intent = "CREATE_EXPENSE",
                Confidence = 0.90,
                Data = new GeminiExtractedData
                {
                    Description = "Sobremesa",
                    Amount = 15m,
                    CategoryName = null
                }
            }
        };

        _geminiServiceMock
            .Setup(g => g.ClassifyIntentAsync(It.IsAny<string>(), It.IsAny<List<ChatMessage>>()))
            .ReturnsAsync(geminiResponse);

        // Simular que a primeira está completa e a segunda não
        var callCount = 0;
        _expenseHandlerMock
            .Setup(h => h.GetMissingFields(It.IsAny<ConversationState>()))
            .Returns(() =>
            {
                callCount++;
                // Primeira chamada: tx1 completa, segunda: tx2 falta categoria
                return callCount == 1 ? new List<string>() : new List<string> { "categoryName" };
            });

        // Act
        var result = await _orchestrator.ProcessMessageAsync(TestChatId, "Gastei 25 de almoço em alimentação e 15 de sobremesa");

        // Assert
        var state = _stateStore.GetOrCreate(TestChatId);
        state.Phase.Should().Be(ConversationPhase.CollectingData);
        state.IsBatchMode.Should().BeTrue();
        result.Should().Contain("Transação 2 de 2");
        result.Should().Contain("Categoria");
    }

    [Fact]
    public async Task ProcessMessage_ShouldPopulateIsPaid_InAllPendingTransactions()
    {
        var geminiResponse = new List<GeminiIntentResponse>
        {
            new()
            {
                Intent = "CREATE_EXPENSE",
                Confidence = 0.95,
                Data = new GeminiExtractedData
                {
                    Description = "Item A",
                    Amount = 10m,
                    CategoryName = "Cat",
                    IsPaid = true
                }
            },
            new()
            {
                Intent = "CREATE_EXPENSE",
                Confidence = 0.90,
                Data = new GeminiExtractedData
                {
                    Description = "Item B",
                    Amount = 20m,
                    CategoryName = "Cat",
                    IsPaid = true
                }
            }
        };

        _geminiServiceMock
            .Setup(g => g.ClassifyIntentAsync(It.IsAny<string>(), It.IsAny<List<ChatMessage>>()))
            .ReturnsAsync(geminiResponse);

        _expenseHandlerMock
            .Setup(h => h.GetMissingFields(It.IsAny<ConversationState>()))
            .Returns(new List<string>());

        await _orchestrator.ProcessMessageAsync(TestChatId, "Duas despesas pagas");

        var state = _stateStore.GetOrCreate(TestChatId);
        state.PendingTransactions.Should().HaveCount(2);
        state.PendingTransactions[0].GetData<bool>("isPaid").Should().BeTrue();
        state.PendingTransactions[1].GetData<bool>("isPaid").Should().BeTrue();
    }

    [Fact]
    public async Task ProcessMessage_ShouldPopulateDueDate_InPendingTransactions()
    {
        var geminiResponse = new List<GeminiIntentResponse>
        {
            new()
            {
                Intent = "CREATE_EXPENSE",
                Confidence = 0.95,
                Data = new GeminiExtractedData
                {
                    Description = "Gasolina",
                    Amount = 60m,
                    CategoryName = "Essenciais",
                    DueDate = new DateTime(2026, 2, 16, 0, 0, 0, DateTimeKind.Utc)
                }
            },
            new()
            {
                Intent = "CREATE_EXPENSE",
                Confidence = 0.90,
                Data = new GeminiExtractedData
                {
                    Description = "Gasolina",
                    Amount = 60m,
                    CategoryName = "Essenciais",
                    DueDate = new DateTime(2026, 2, 23, 0, 0, 0, DateTimeKind.Utc)
                }
            }
        };

        _geminiServiceMock
            .Setup(g => g.ClassifyIntentAsync(It.IsAny<string>(), It.IsAny<List<ChatMessage>>()))
            .ReturnsAsync(geminiResponse);

        _expenseHandlerMock
            .Setup(h => h.GetMissingFields(It.IsAny<ConversationState>()))
            .Returns(new List<string>());

        await _orchestrator.ProcessMessageAsync(TestChatId, "Lance 2 despesas de gasolina");

        var state = _stateStore.GetOrCreate(TestChatId);
        state.PendingTransactions.Should().HaveCount(2);
        state.PendingTransactions[0].GetData<DateTime>("dueDate").Should().Be(new DateTime(2026, 2, 16, 0, 0, 0, DateTimeKind.Utc));
        state.PendingTransactions[1].GetData<DateTime>("dueDate").Should().Be(new DateTime(2026, 2, 23, 0, 0, 0, DateTimeKind.Utc));
    }

    #endregion

    #region Confirmation Phase

    [Fact]
    public async Task ProcessMessage_ShouldExecuteAll_WhenBatchConfirmed()
    {
        // Arrange: set up state with 2 pending
        var state = _stateStore.GetOrCreate(TestChatId);
        state.Phase = ConversationPhase.AwaitingConfirmation;

        var pending1 = new PendingTransaction { Intent = IntentType.CreateExpense };
        pending1.SetData("description", "Item A");
        pending1.SetData("amount", 10m);
        pending1.SetData("categoryName", "Cat");
        pending1.SetData("isPaid", true);

        var pending2 = new PendingTransaction { Intent = IntentType.CreateExpense };
        pending2.SetData("description", "Item B");
        pending2.SetData("amount", 20m);
        pending2.SetData("categoryName", "Cat");
        pending2.SetData("isPaid", false);

        state.PendingTransactions.AddRange([pending1, pending2]);

        _expenseHandlerMock
            .Setup(h => h.HandleConfirmationAsync(It.IsAny<ConversationState>(), true))
            .ReturnsAsync("✅ Sucesso");

        // Act
        var result = await _orchestrator.ProcessMessageAsync(TestChatId, "sim");

        // Assert
        _expenseHandlerMock.Verify(
            h => h.HandleConfirmationAsync(It.IsAny<ConversationState>(), true),
            Times.Exactly(2));

        result.Should().Contain("Sucesso");
        state.Phase.Should().Be(ConversationPhase.Idle); // reset happened
    }

    [Theory]
    [InlineData("não")]
    [InlineData("nao")]
    [InlineData("n")]
    public async Task ProcessMessage_ShouldCancel_WhenConfirmationDenied(string denial)
    {
        var state = _stateStore.GetOrCreate(TestChatId);
        state.Phase = ConversationPhase.AwaitingConfirmation;
        state.PendingTransactions.Add(new PendingTransaction { Intent = IntentType.CreateExpense });

        var result = await _orchestrator.ProcessMessageAsync(TestChatId, denial);

        result.Should().Contain("cancelada");
        state.Phase.Should().Be(ConversationPhase.Idle);
    }

    [Fact]
    public async Task ProcessMessage_ShouldAskAgain_WhenConfirmationUnrecognized()
    {
        var state = _stateStore.GetOrCreate(TestChatId);
        state.Phase = ConversationPhase.AwaitingConfirmation;
        state.PendingTransactions.Add(new PendingTransaction { Intent = IntentType.CreateExpense });

        var result = await _orchestrator.ProcessMessageAsync(TestChatId, "talvez");

        result.Should().Contain("sim");
        result.Should().Contain("não");
    }

    [Fact]
    public async Task ProcessMessage_ShouldAllowEditingCategoryInBatch_WhenAwaitingConfirmation()
    {
        var state = _stateStore.GetOrCreate(TestChatId);
        state.Phase = ConversationPhase.AwaitingConfirmation;

        var pending1 = new PendingTransaction { Intent = IntentType.CreateExpense };
        pending1.SetData("description", "Lat chease");
        pending1.SetData("amount", 31.00m);
        pending1.SetData("categoryName", "Não planejado");
        pending1.SetData("isPaid", true);

        var pending2 = new PendingTransaction { Intent = IntentType.CreateExpense };
        pending2.SetData("description", "Supermercados Myata");
        pending2.SetData("amount", 31.72m);
        pending2.SetData("categoryName", "Supermercados");
        pending2.SetData("isPaid", true);

        state.PendingTransactions.AddRange([pending1, pending2]);

        _geminiServiceMock
            .Setup(g => g.ExtractDataFromFollowUpAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new GeminiIntentResponse
            {
                Intent = "CONTINUE",
                Data = new GeminiExtractedData
                {
                    CategoryName = "Essenciais"
                }
            });

        _expenseHandlerMock
            .Setup(h => h.GetMissingFields(It.IsAny<ConversationState>()))
            .Returns(new List<string>());

        var result = await _orchestrator.ProcessMessageAsync(TestChatId, "altere a categoria do item 2 para Essenciais");

        state.Phase.Should().Be(ConversationPhase.AwaitingConfirmation);
        state.PendingTransactions[1].GetData<string>("categoryName").Should().Be("Essenciais");
        result.Should().Contain("Item 2 atualizado");
        result.Should().Contain("Essenciais");
    }

    [Fact]
    public async Task ProcessMessage_ShouldAllowEditingDescriptionInBatch_WhenAwaitingConfirmation()
    {
        var state = _stateStore.GetOrCreate(TestChatId);
        state.Phase = ConversationPhase.AwaitingConfirmation;

        var pending1 = new PendingTransaction { Intent = IntentType.CreateExpense };
        pending1.SetData("description", "Item A");
        pending1.SetData("amount", 10m);
        pending1.SetData("categoryName", "Cat");
        pending1.SetData("isPaid", true);

        var pending2 = new PendingTransaction { Intent = IntentType.CreateExpense };
        pending2.SetData("description", "Supermercados Myata Lages Bra");
        pending2.SetData("amount", 31.72m);
        pending2.SetData("categoryName", "Supermercados");
        pending2.SetData("isPaid", true);

        state.PendingTransactions.AddRange([pending1, pending2]);

        _geminiServiceMock
            .Setup(g => g.ExtractDataFromFollowUpAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new GeminiIntentResponse
            {
                Intent = "CONTINUE",
                Data = new GeminiExtractedData
                {
                    Description = "Myatã"
                }
            });

        _expenseHandlerMock
            .Setup(h => h.GetMissingFields(It.IsAny<ConversationState>()))
            .Returns(new List<string>());

        var result = await _orchestrator.ProcessMessageAsync(TestChatId, "altere a descrição do item 2 para Myatã");

        state.Phase.Should().Be(ConversationPhase.AwaitingConfirmation);
        state.PendingTransactions[1].GetData<string>("description").Should().Be("Myatã");
        result.Should().Contain("Item 2 atualizado");
        result.Should().Contain("Myatã");
    }

    #endregion

    #region Cancel During Collecting Phase

    [Theory]
    [InlineData("/cancelar")]
    [InlineData("/cancel")]
    public async Task ProcessMessage_ShouldCancel_WhenCancelDuringCollecting(string command)
    {
        var state = _stateStore.GetOrCreate(TestChatId);
        state.Phase = ConversationPhase.CollectingData;
        state.PendingTransactions.Add(new PendingTransaction { Intent = IntentType.CreateExpense });

        var result = await _orchestrator.ProcessMessageAsync(TestChatId, command);

        result.Should().Contain("cancelada");
        state.Phase.Should().Be(ConversationPhase.Idle);
    }

    #endregion

    #region Financial Consulting (No Confirmation)

    [Fact]
    public async Task ProcessMessage_ShouldExecuteDirectly_WhenFinancialConsulting()
    {
        var geminiResponse = new List<GeminiIntentResponse>
        {
            new()
            {
                Intent = "FINANCIAL_CONSULTING",
                Confidence = 0.95,
                Data = new GeminiExtractedData
                {
                    Question = "Como estou esse mês?"
                }
            }
        };

        _geminiServiceMock
            .Setup(g => g.ClassifyIntentAsync(It.IsAny<string>(), It.IsAny<List<ChatMessage>>()))
            .ReturnsAsync(geminiResponse);

        _consultingHandlerMock
            .Setup(h => h.HandleAsync(It.IsAny<ConversationState>()))
            .ReturnsAsync("📊 Análise financeira...");

        var result = await _orchestrator.ProcessMessageAsync(TestChatId, "Como estou esse mês?");

        result.Should().Contain("Análise financeira");
        _consultingHandlerMock.Verify(h => h.HandleAsync(It.IsAny<ConversationState>()), Times.Once);

        var state = _stateStore.GetOrCreate(TestChatId);
        state.Phase.Should().Be(ConversationPhase.Idle);
    }

    #endregion

    #region Gemini Returns Null / Unknown Intent

    [Fact]
    public async Task ProcessMessage_ShouldReturnError_WhenGeminiReturnsNull()
    {
        _geminiServiceMock
            .Setup(g => g.ClassifyIntentAsync(It.IsAny<string>(), It.IsAny<List<ChatMessage>>()))
            .ReturnsAsync((List<GeminiIntentResponse>?)null);

        var result = await _orchestrator.ProcessMessageAsync(TestChatId, "xyz abc");

        result.Should().Contain("Não consegui entender");
    }

    [Fact]
    public async Task ProcessMessage_ShouldReturnHelper_WhenAllIntentsUnknown()
    {
        var geminiResponse = new List<GeminiIntentResponse>
        {
            new() { Intent = "INVALID_INTENT", Confidence = 0.5 }
        };

        _geminiServiceMock
            .Setup(g => g.ClassifyIntentAsync(It.IsAny<string>(), It.IsAny<List<ChatMessage>>()))
            .ReturnsAsync(geminiResponse);

        var result = await _orchestrator.ProcessMessageAsync(TestChatId, "Olá bom dia");

        result.Should().Contain("Não entendi");
    }

    #endregion

    #region Collecting Phase — Follow-up Data

    [Fact]
    public async Task ProcessMessage_ShouldMergeExtraction_WhenCollectingData()
    {
        // Setup: state already in collecting phase, pending tx missing categoryName
        var state = _stateStore.GetOrCreate(TestChatId);
        state.Phase = ConversationPhase.CollectingData;
        state.CurrentTransactionIndex = 0;

        var pending = new PendingTransaction { Intent = IntentType.CreateExpense };
        pending.SetData("description", "Almoço");
        pending.SetData("amount", 45m);
        state.PendingTransactions.Add(pending);

        // Gemini follow-up extraction
        var extraction = new GeminiIntentResponse
        {
            Intent = "CONTINUE",
            Data = new GeminiExtractedData { CategoryName = "Alimentação" }
        };

        _geminiServiceMock
            .Setup(g => g.ExtractDataFromFollowUpAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(extraction);

        // Now all fields present
        _expenseHandlerMock
            .Setup(h => h.GetMissingFields(It.IsAny<ConversationState>()))
            .Returns(new List<string>());

        _expenseHandlerMock
            .Setup(h => h.BuildConfirmationMessage(It.IsAny<ConversationState>()))
            .Returns("Confirma?");

        // Act
        var result = await _orchestrator.ProcessMessageAsync(TestChatId, "Alimentação");

        // Assert
        result.Should().Contain("Confirma");
        state.Phase.Should().Be(ConversationPhase.AwaitingConfirmation);
        state.PendingTransactions[0].GetData<string>("categoryName").Should().Be("Alimentação");
    }

    #endregion

    #region Mixed Intents (Expense + Card Purchase)

    [Fact]
    public async Task ProcessMessage_ShouldHandleMixedIntents_InBatch()
    {
        var geminiResponse = new List<GeminiIntentResponse>
        {
            new()
            {
                Intent = "CREATE_EXPENSE",
                Confidence = 0.95,
                Data = new GeminiExtractedData
                {
                    Description = "Internet",
                    Amount = 120m,
                    CategoryName = "Essenciais",
                    IsPaid = true
                }
            },
            new()
            {
                Intent = "CREATE_CARD_PURCHASE",
                Confidence = 0.90,
                Data = new GeminiExtractedData
                {
                    Description = "Notebook",
                    Amount = 3000m,
                    CardName = "Nubank",
                    Installments = 10
                }
            }
        };

        _geminiServiceMock
            .Setup(g => g.ClassifyIntentAsync(It.IsAny<string>(), It.IsAny<List<ChatMessage>>()))
            .ReturnsAsync(geminiResponse);

        _expenseHandlerMock
            .Setup(h => h.GetMissingFields(It.IsAny<ConversationState>()))
            .Returns(new List<string>());

        _cardPurchaseHandlerMock
            .Setup(h => h.GetMissingFields(It.IsAny<ConversationState>()))
            .Returns(new List<string>());

        var result = await _orchestrator.ProcessMessageAsync(TestChatId,
            "Paguei 120 de internet e comprei notebook 3000 em 10x no nubank");

        var state = _stateStore.GetOrCreate(TestChatId);
        state.Phase.Should().Be(ConversationPhase.AwaitingConfirmation);
        state.PendingTransactions.Should().HaveCount(2);
        state.PendingTransactions[0].Intent.Should().Be(IntentType.CreateExpense);
        state.PendingTransactions[1].Intent.Should().Be(IntentType.CreateCardPurchase);
        result.Should().Contain("2 lançamentos");
    }

    #endregion

    #region Media Processing (Image/Audio)

    [Fact]
    public async Task ProcessMediaMessage_ShouldExtractTransactionFromImage_WhenGeminiReturnsData()
    {
        // Arrange
        var geminiResponse = new List<GeminiIntentResponse>
        {
            new()
            {
                Intent = "CREATE_EXPENSE",
                Confidence = 0.90,
                Data = new GeminiExtractedData
                {
                    Description = "Supermercado",
                    Amount = 150.00m,
                    CategoryName = "Alimentação",
                    IsPaid = true
                }
            }
        };

        _geminiServiceMock
            .Setup(g => g.ClassifyIntentFromMediaAsync(It.IsAny<byte[]>(), "image/jpeg", null, It.IsAny<List<ChatMessage>>()))
            .ReturnsAsync(geminiResponse);

        _expenseHandlerMock
            .Setup(h => h.GetMissingFields(It.IsAny<ConversationState>()))
            .Returns(new List<string>());

        _expenseHandlerMock
            .Setup(h => h.BuildConfirmationMessage(It.IsAny<ConversationState>()))
            .Returns("Confirma o lançamento?");

        // Act
        var result = await _orchestrator.ProcessMediaMessageAsync(TestChatId, new byte[] { 1, 2, 3 }, "image/jpeg", null);

        // Assert
        result.Should().Contain("Confirma");
        var state = _stateStore.GetOrCreate(TestChatId);
        state.Phase.Should().Be(ConversationPhase.AwaitingConfirmation);
        state.PendingTransactions.Should().HaveCount(1);
        state.PendingTransactions[0].GetData<string>("description").Should().Be("Supermercado");
    }

    [Fact]
    public async Task ProcessMediaMessage_ShouldExtractTransactionFromAudio_WhenGeminiReturnsData()
    {
        // Arrange
        var geminiResponse = new List<GeminiIntentResponse>
        {
            new()
            {
                Intent = "CREATE_EXPENSE",
                Confidence = 0.85,
                Data = new GeminiExtractedData
                {
                    Description = "Almoço",
                    Amount = 35m,
                    CategoryName = "Alimentação"
                }
            }
        };

        _geminiServiceMock
            .Setup(g => g.ClassifyIntentFromMediaAsync(It.IsAny<byte[]>(), "audio/ogg", null, It.IsAny<List<ChatMessage>>()))
            .ReturnsAsync(geminiResponse);

        _expenseHandlerMock
            .Setup(h => h.GetMissingFields(It.IsAny<ConversationState>()))
            .Returns(new List<string>());

        _expenseHandlerMock
            .Setup(h => h.BuildConfirmationMessage(It.IsAny<ConversationState>()))
            .Returns("Confirma o lançamento?");

        // Act
        var result = await _orchestrator.ProcessMediaMessageAsync(TestChatId, new byte[] { 1, 2, 3 }, "audio/ogg", null);

        // Assert
        result.Should().Contain("Confirma");
        var state = _stateStore.GetOrCreate(TestChatId);
        state.PendingTransactions.Should().HaveCount(1);
        state.PendingTransactions[0].GetData<decimal>("amount").Should().Be(35m);
    }

    [Fact]
    public async Task ProcessMediaMessage_ShouldReturnError_WhenGeminiReturnsNull()
    {
        _geminiServiceMock
            .Setup(g => g.ClassifyIntentFromMediaAsync(It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<ChatMessage>>()))
            .ReturnsAsync((List<GeminiIntentResponse>?)null);

        var result = await _orchestrator.ProcessMediaMessageAsync(TestChatId, new byte[] { 1, 2, 3 }, "image/jpeg", null);

        result.Should().Contain("Não consegui extrair informações");
        result.Should().Contain("imagem");
    }

    [Fact]
    public async Task ProcessMediaMessage_ShouldReturnAudioError_WhenAudioGeminiReturnsNull()
    {
        _geminiServiceMock
            .Setup(g => g.ClassifyIntentFromMediaAsync(It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<ChatMessage>>()))
            .ReturnsAsync((List<GeminiIntentResponse>?)null);

        var result = await _orchestrator.ProcessMediaMessageAsync(TestChatId, new byte[] { 1, 2, 3 }, "audio/ogg", null);

        result.Should().Contain("Não consegui extrair informações");
        result.Should().Contain("áudio");
    }

    [Fact]
    public async Task ProcessMediaMessage_ShouldAskMissingFields_WhenImageDataIncomplete()
    {
        var geminiResponse = new List<GeminiIntentResponse>
        {
            new()
            {
                Intent = "CREATE_EXPENSE",
                Confidence = 0.80,
                Data = new GeminiExtractedData
                {
                    Description = "Compra",
                    Amount = 99m,
                    CategoryName = null
                }
            }
        };

        _geminiServiceMock
            .Setup(g => g.ClassifyIntentFromMediaAsync(It.IsAny<byte[]>(), "image/jpeg", null, It.IsAny<List<ChatMessage>>()))
            .ReturnsAsync(geminiResponse);

        _expenseHandlerMock
            .Setup(h => h.GetMissingFields(It.IsAny<ConversationState>()))
            .Returns(new List<string> { "categoryName" });

        var result = await _orchestrator.ProcessMediaMessageAsync(TestChatId, new byte[] { 1, 2, 3 }, "image/jpeg", null);

        result.Should().Contain("Categoria");
        var state = _stateStore.GetOrCreate(TestChatId);
        state.Phase.Should().Be(ConversationPhase.CollectingData);
    }

    #endregion

    #region Category Listing When Missing

    [Fact]
    public async Task ProcessMessage_ShouldListCategories_WhenCategoryMissing()
    {
        // Arrange: categories exist in the repository
        var categories = new List<Category>
        {
            new("Alimentação", "#4CAF50"),
            new("Saúde", "#2196F3"),
            new("Lazer", "#FF9800")
        };

        _categoryRepositoryMock
            .Setup(r => r.GetAllAsync())
            .ReturnsAsync(categories);

        var geminiResponse = new List<GeminiIntentResponse>
        {
            new()
            {
                Intent = "CREATE_EXPENSE",
                Confidence = 0.95,
                Data = new GeminiExtractedData
                {
                    Description = "Almoço",
                    Amount = 45m,
                    CategoryName = null
                }
            }
        };

        _geminiServiceMock
            .Setup(g => g.ClassifyIntentAsync(It.IsAny<string>(), It.IsAny<List<ChatMessage>>()))
            .ReturnsAsync(geminiResponse);

        _expenseHandlerMock
            .Setup(h => h.GetMissingFields(It.IsAny<ConversationState>()))
            .Returns(new List<string> { "categoryName" });

        // Act
        var result = await _orchestrator.ProcessMessageAsync(TestChatId, "Gastei 45 de almoço");

        // Assert
        result.Should().Contain("Categoria");
        result.Should().Contain("Alimentação");
        result.Should().Contain("Saúde");
        result.Should().Contain("Lazer");
    }

    [Fact]
    public async Task ProcessMessage_ShouldShowGenericPrompt_WhenNoCategoriesExist()
    {
        // Arrange: no categories
        _categoryRepositoryMock
            .Setup(r => r.GetAllAsync())
            .ReturnsAsync(Enumerable.Empty<Category>());

        var geminiResponse = new List<GeminiIntentResponse>
        {
            new()
            {
                Intent = "CREATE_EXPENSE",
                Confidence = 0.95,
                Data = new GeminiExtractedData
                {
                    Description = "Almoço",
                    Amount = 45m,
                    CategoryName = null
                }
            }
        };

        _geminiServiceMock
            .Setup(g => g.ClassifyIntentAsync(It.IsAny<string>(), It.IsAny<List<ChatMessage>>()))
            .ReturnsAsync(geminiResponse);

        _expenseHandlerMock
            .Setup(h => h.GetMissingFields(It.IsAny<ConversationState>()))
            .Returns(new List<string> { "categoryName" });

        // Act
        var result = await _orchestrator.ProcessMessageAsync(TestChatId, "Gastei 45 de almoço");

        // Assert
        result.Should().Contain("Categoria");
        result.Should().Contain("Alimentação"); // from the generic example
    }

    #endregion

    #region Help Message Includes Media Support

    [Fact]
    public async Task ProcessMessage_HelpShouldMentionImageAndAudio()
    {
        var result = await _orchestrator.ProcessMessageAsync(TestChatId, "/start");

        result.Should().Contain("imagem");
        result.Should().Contain("áudio");
    }

    #endregion
}
