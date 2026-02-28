using System.Collections.Concurrent;
using Microsoft.SemanticKernel.ChatCompletion;

namespace Jullius.ServiceApi.Telegram;

/// <summary>
/// Armazena o ChatHistory por chatId do Telegram com TTL e limpeza periódica.
/// Substitui o antigo ConversationStateStore.
/// </summary>
public sealed class ChatHistoryStore : IDisposable
{
    private readonly ConcurrentDictionary<long, ChatHistoryEntry> _histories = new();
    private readonly TimeSpan _ttl = TimeSpan.FromMinutes(30);
    private readonly Timer _cleanupTimer;
    private readonly int _maxMessagesPerChat;

    public ChatHistoryStore(int maxMessagesPerChat = 50)
    {
        _maxMessagesPerChat = maxMessagesPerChat;
        _cleanupTimer = new Timer(CleanupExpired, null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
    }

    /// <summary>
    /// Retorna o ChatHistory existente ou cria um novo para o chatId informado.
    /// </summary>
    public ChatHistory GetOrCreate(long chatId)
    {
        var entry = _histories.GetOrAdd(chatId, _ => new ChatHistoryEntry());
        entry.Touch();
        return entry.History;
    }

    /// <summary>
    /// Adiciona uma mensagem do usuário e faz trim se necessário.
    /// </summary>
    public void AddUserMessage(long chatId, string message)
    {
        var history = GetOrCreate(chatId);
        history.AddUserMessage(message);
        TrimIfNeeded(history);
    }

    /// <summary>
    /// Adiciona uma mensagem do assistente e faz trim se necessário.
    /// </summary>
    public void AddAssistantMessage(long chatId, string message)
    {
        var history = GetOrCreate(chatId);
        history.AddAssistantMessage(message);
        TrimIfNeeded(history);
    }

    /// <summary>
    /// Remove o histórico de um chatId específico.
    /// </summary>
    public void Remove(long chatId) => _histories.TryRemove(chatId, out _);

    /// <summary>
    /// Remove todas as mensagens exceto a system message (se houver).
    /// </summary>
    public void Clear(long chatId)
    {
        if (_histories.TryGetValue(chatId, out var entry))
        {
            var systemMessages = entry.History
                .Where(m => m.Role == AuthorRole.System)
                .ToList();

            entry.History.Clear();

            foreach (var msg in systemMessages)
                entry.History.Add(msg);

            entry.Touch();
        }
    }

    private void TrimIfNeeded(ChatHistory history)
    {
        // Preserva system messages no início
        var systemCount = history.TakeWhile(m => m.Role == AuthorRole.System).Count();
        var nonSystemCount = history.Count - systemCount;

        while (nonSystemCount > _maxMessagesPerChat)
        {
            history.RemoveAt(systemCount); // remove a mensagem não-system mais antiga
            nonSystemCount--;
        }
    }

    private void CleanupExpired(object? state)
    {
        var cutoff = DateTime.UtcNow - _ttl;
        var expired = _histories
            .Where(kv => kv.Value.LastActivity < cutoff)
            .Select(kv => kv.Key)
            .ToList();

        foreach (var chatId in expired)
            _histories.TryRemove(chatId, out _);
    }

    /// <summary>
    /// Aplica trim ao histórico de um chatId, removendo mensagens antigas se necessário.
    /// Útil quando mensagens são adicionadas diretamente ao ChatHistory compartilhado
    /// (fora dos métodos AddUserMessage/AddAssistantMessage).
    /// </summary>
    public void TrimHistory(long chatId)
    {
        var history = GetOrCreate(chatId);
        TrimIfNeeded(history);
    }

    public void Dispose()
    {
        _cleanupTimer.Dispose();
    }

    private sealed class ChatHistoryEntry
    {
        public ChatHistory History { get; } = [];
        public DateTime LastActivity { get; private set; } = DateTime.UtcNow;
        public void Touch() => LastActivity = DateTime.UtcNow;
    }
}
