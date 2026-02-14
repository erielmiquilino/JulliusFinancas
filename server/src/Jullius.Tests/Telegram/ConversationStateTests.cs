using FluentAssertions;
using Jullius.ServiceApi.Telegram;
using Xunit;

namespace Jullius.Tests.Telegram;

public class ConversationStateTests
{
    #region SetData / GetData / HasData

    [Fact]
    public void SetData_ShouldStore_AndGetData_ShouldRetrieve()
    {
        var state = new ConversationState();

        state.SetData("description", "Almoço");
        state.SetData("amount", 45.90m);

        state.GetData<string>("description").Should().Be("Almoço");
        state.GetData<decimal>("amount").Should().Be(45.90m);
    }

    [Fact]
    public void HasData_ShouldReturnFalse_WhenKeyNotSet()
    {
        var state = new ConversationState();

        state.HasData("description").Should().BeFalse();
    }

    [Fact]
    public void HasData_ShouldReturnFalse_WhenValueIsNull()
    {
        var state = new ConversationState();
        state.SetData("description", null);

        state.HasData("description").Should().BeFalse();
    }

    [Fact]
    public void HasData_ShouldReturnTrue_WhenValueIsSet()
    {
        var state = new ConversationState();
        state.SetData("description", "Teste");

        state.HasData("description").Should().BeTrue();
    }

    [Fact]
    public void GetData_ShouldReturnDefault_WhenTypeMismatch()
    {
        var state = new ConversationState();
        state.SetData("amount", "not a decimal");

        state.GetData<decimal>("amount").Should().Be(0m);
    }

    #endregion

    #region Reset

    [Fact]
    public void Reset_ShouldClearAllState()
    {
        var state = new ConversationState
        {
            Phase = ConversationPhase.AwaitingConfirmation,
            CurrentIntent = IntentType.CreateExpense
        };
        state.SetData("description", "Teste");
        state.History.Add(new ChatMessage { Role = "user", Content = "Olá" });
        state.PendingTransactions.Add(new PendingTransaction { Intent = IntentType.CreateExpense });
        state.CurrentTransactionIndex = 2;

        state.Reset();

        state.Phase.Should().Be(ConversationPhase.Idle);
        state.CurrentIntent.Should().BeNull();
        state.CollectedData.Should().BeEmpty();
        state.History.Should().BeEmpty();
        state.PendingTransactions.Should().BeEmpty();
        state.CurrentTransactionIndex.Should().Be(0);
    }

    #endregion

    #region IsBatchMode

    [Fact]
    public void IsBatchMode_ShouldReturnFalse_WhenSingleTransaction()
    {
        var state = new ConversationState();
        state.PendingTransactions.Add(new PendingTransaction());

        state.IsBatchMode.Should().BeFalse();
    }

    [Fact]
    public void IsBatchMode_ShouldReturnTrue_WhenMultipleTransactions()
    {
        var state = new ConversationState();
        state.PendingTransactions.Add(new PendingTransaction());
        state.PendingTransactions.Add(new PendingTransaction());

        state.IsBatchMode.Should().BeTrue();
    }

    #endregion

    #region LoadFromPending / SaveToPending

    [Fact]
    public void LoadFromPending_ShouldCopyDataAndIntent()
    {
        var state = new ConversationState();
        var pending = new PendingTransaction { Intent = IntentType.CreateExpense };
        pending.SetData("description", "Almoço");
        pending.SetData("amount", 50m);
        pending.SetData("isPaid", true);

        state.LoadFromPending(pending);

        state.CurrentIntent.Should().Be(IntentType.CreateExpense);
        state.GetData<string>("description").Should().Be("Almoço");
        state.GetData<decimal>("amount").Should().Be(50m);
        state.GetData<bool>("isPaid").Should().BeTrue();
    }

    [Fact]
    public void SaveToPending_ShouldCopyDataBack()
    {
        var state = new ConversationState();
        var pending = new PendingTransaction { Intent = IntentType.CreateExpense };
        state.PendingTransactions.Add(pending);

        state.SetData("description", "Café");
        state.SetData("amount", 15m);

        state.SaveToPending(0);

        state.PendingTransactions[0].GetData<string>("description").Should().Be("Café");
        state.PendingTransactions[0].GetData<decimal>("amount").Should().Be(15m);
    }

    [Fact]
    public void SaveToPending_ShouldNotThrow_WhenIndexOutOfRange()
    {
        var state = new ConversationState();
        state.SetData("test", "value");

        var act = () => state.SaveToPending(5);

        act.Should().NotThrow();
    }

    #endregion

    #region Touch

    [Fact]
    public void Touch_ShouldUpdateLastActivity()
    {
        var state = new ConversationState();
        var before = state.LastActivity;

        Thread.Sleep(10);
        state.Touch();

        state.LastActivity.Should().BeAfter(before);
    }

    #endregion
}

public class PendingTransactionTests
{
    [Fact]
    public void SetData_GetData_HasData_ShouldWorkCorrectly()
    {
        var pending = new PendingTransaction();

        pending.HasData("amount").Should().BeFalse();

        pending.SetData("amount", 100m);
        pending.SetData("isPaid", true);

        pending.HasData("amount").Should().BeTrue();
        pending.GetData<decimal>("amount").Should().Be(100m);
        pending.GetData<bool>("isPaid").Should().BeTrue();
    }

    [Fact]
    public void GetData_ShouldReturnDefault_WhenKeyMissing()
    {
        var pending = new PendingTransaction();

        pending.GetData<string>("description").Should().BeNull();
        pending.GetData<decimal>("amount").Should().Be(0m);
        pending.GetData<bool>("isPaid").Should().BeFalse();
    }
}
