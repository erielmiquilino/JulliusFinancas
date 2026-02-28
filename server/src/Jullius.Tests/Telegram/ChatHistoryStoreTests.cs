using FluentAssertions;
using Jullius.ServiceApi.Telegram;
using Microsoft.SemanticKernel.ChatCompletion;
using Xunit;

namespace Jullius.Tests.Telegram;

/// <summary>
/// Testes para o ChatHistoryStore que gerencia histórico de conversas por chatId.
/// </summary>
public class ChatHistoryStoreTests : IDisposable
{
    private readonly ChatHistoryStore _store;

    public ChatHistoryStoreTests()
    {
        _store = new ChatHistoryStore(maxMessagesPerChat: 5);
    }

    [Fact]
    public void GetOrCreate_ShouldReturnNewHistory_WhenChatIdNotExists()
    {
        var history = _store.GetOrCreate(123);

        Assert.NotNull(history);
        Assert.Empty(history);
    }

    [Fact]
    public void GetOrCreate_ShouldReturnSameInstance_WhenCalledTwice()
    {
        var history1 = _store.GetOrCreate(123);
        var history2 = _store.GetOrCreate(123);

        Assert.Same(history1, history2);
    }

    [Fact]
    public void GetOrCreate_ShouldReturnDifferentInstances_ForDifferentChatIds()
    {
        var history1 = _store.GetOrCreate(1);
        var history2 = _store.GetOrCreate(2);

        Assert.NotSame(history1, history2);
    }

    [Fact]
    public void AddUserMessage_ShouldAddToHistory()
    {
        _store.AddUserMessage(1, "Olá");

        var history = _store.GetOrCreate(1);
        Assert.Single(history);
        Assert.Equal(AuthorRole.User, history[0].Role);
        Assert.Equal("Olá", history[0].Content);
    }

    [Fact]
    public void AddAssistantMessage_ShouldAddToHistory()
    {
        _store.AddAssistantMessage(1, "Olá! Como posso ajudar?");

        var history = _store.GetOrCreate(1);
        Assert.Single(history);
        Assert.Equal(AuthorRole.Assistant, history[0].Role);
    }

    [Fact]
    public void TrimIfNeeded_ShouldRemoveOldestMessages_WhenOverLimit()
    {
        // maxMessagesPerChat = 5
        for (int i = 1; i <= 7; i++)
            _store.AddUserMessage(1, $"Mensagem {i}");

        var history = _store.GetOrCreate(1);
        Assert.Equal(5, history.Count);
        Assert.Equal("Mensagem 3", history[0].Content);
        Assert.Equal("Mensagem 7", history[^1].Content);
    }

    [Fact]
    public void TrimIfNeeded_ShouldPreserveSystemMessages()
    {
        var history = _store.GetOrCreate(1);
        history.AddSystemMessage("System prompt");

        for (int i = 1; i <= 6; i++)
            _store.AddUserMessage(1, $"Msg {i}");

        Assert.Equal(AuthorRole.System, history[0].Role);
        Assert.Equal("System prompt", history[0].Content);
    }

    [Fact]
    public void Remove_ShouldDeleteChatHistory()
    {
        _store.AddUserMessage(1, "Teste");
        _store.Remove(1);

        var history = _store.GetOrCreate(1);
        Assert.Empty(history);
    }

    [Fact]
    public void Clear_ShouldResetHistory_ButPreserveSystemMessages()
    {
        var history = _store.GetOrCreate(1);
        history.AddSystemMessage("System prompt");
        _store.AddUserMessage(1, "Olá");
        _store.AddAssistantMessage(1, "Oi!");

        _store.Clear(1);

        Assert.Single(history);
        Assert.Equal(AuthorRole.System, history[0].Role);
    }

    public void Dispose()
    {
        _store.Dispose();
    }
}
