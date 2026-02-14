using FluentAssertions;
using Jullius.ServiceApi.Telegram;
using Xunit;

namespace Jullius.Tests.Telegram;

public class ConversationStateStoreTests
{
    [Fact]
    public void GetOrCreate_ShouldReturnSameState_ForSameChatId()
    {
        var store = new ConversationStateStore();

        var state1 = store.GetOrCreate(123);
        var state2 = store.GetOrCreate(123);

        state1.Should().BeSameAs(state2);
    }

    [Fact]
    public void GetOrCreate_ShouldReturnDifferentState_ForDifferentChatIds()
    {
        var store = new ConversationStateStore();

        var state1 = store.GetOrCreate(123);
        var state2 = store.GetOrCreate(456);

        state1.Should().NotBeSameAs(state2);
    }

    [Fact]
    public void GetOrCreate_ShouldInitializeWithIdlePhase()
    {
        var store = new ConversationStateStore();

        var state = store.GetOrCreate(123);

        state.ChatId.Should().Be(123);
        state.Phase.Should().Be(ConversationPhase.Idle);
        state.CurrentIntent.Should().BeNull();
    }

    [Fact]
    public void Remove_ShouldDeleteState()
    {
        var store = new ConversationStateStore();
        var state1 = store.GetOrCreate(123);
        state1.SetData("test", "value");

        store.Remove(123);

        var state2 = store.GetOrCreate(123);
        state2.HasData("test").Should().BeFalse(); // novo state, sem dados
    }
}
