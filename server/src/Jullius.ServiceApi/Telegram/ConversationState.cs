namespace Jullius.ServiceApi.Telegram;

public enum ConversationPhase
{
    Idle,
    CollectingData,
    AwaitingConfirmation
}

public enum IntentType
{
    CreateExpense,
    CreateCardPurchase,
    FinancialConsulting,
    Unknown
}

/// <summary>
/// Representa uma transação pendente num fluxo de múltiplas transações.
/// </summary>
public class PendingTransaction
{
    public IntentType Intent { get; set; }
    public Dictionary<string, object?> Data { get; set; } = new();

    public void SetData(string key, object? value) => Data[key] = value;
    public bool HasData(string key) => Data.ContainsKey(key) && Data[key] != null;
    public T? GetData<T>(string key) => Data.TryGetValue(key, out var v) && v is T typed ? typed : default;
}

public class ConversationState
{
    public long ChatId { get; set; }
    public ConversationPhase Phase { get; set; } = ConversationPhase.Idle;
    public IntentType? CurrentIntent { get; set; }
    public Dictionary<string, object?> CollectedData { get; set; } = new();
    public DateTime LastActivity { get; set; } = DateTime.UtcNow;
    public List<ChatMessage> History { get; set; } = [];

    /// <summary>
    /// Fila de transações pendentes para fluxos de múltiplas transações.
    /// </summary>
    public List<PendingTransaction> PendingTransactions { get; set; } = [];

    /// <summary>
    /// Índice da transação atualmente sendo coletada (quando há dados faltantes no batch).
    /// </summary>
    public int CurrentTransactionIndex { get; set; }

    public bool IsBatchMode => PendingTransactions.Count > 1;

    public void Reset()
    {
        Phase = ConversationPhase.Idle;
        CurrentIntent = null;
        CollectedData.Clear();
        PendingTransactions.Clear();
        CurrentTransactionIndex = 0;
        History.Clear();
        Touch();
    }

    public void Touch() => LastActivity = DateTime.UtcNow;

    public T? GetData<T>(string key)
    {
        if (CollectedData.TryGetValue(key, out var value) && value is T typed)
            return typed;
        return default;
    }

    public void SetData(string key, object? value)
    {
        CollectedData[key] = value;
        Touch();
    }

    public bool HasData(string key) => CollectedData.ContainsKey(key) && CollectedData[key] != null;

    /// <summary>
    /// Carrega os dados de uma PendingTransaction para o CollectedData atual.
    /// </summary>
    public void LoadFromPending(PendingTransaction pending)
    {
        CurrentIntent = pending.Intent;
        CollectedData = new Dictionary<string, object?>(pending.Data);
    }

    /// <summary>
    /// Salva o CollectedData atual de volta na PendingTransaction correspondente.
    /// </summary>
    public void SaveToPending(int index)
    {
        if (index < PendingTransactions.Count)
            PendingTransactions[index].Data = new Dictionary<string, object?>(CollectedData);
    }
}

public class ChatMessage
{
    public string Role { get; set; } = string.Empty; // "user" | "assistant"
    public string Content { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
