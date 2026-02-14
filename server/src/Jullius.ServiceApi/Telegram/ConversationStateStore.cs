using System.Collections.Concurrent;

namespace Jullius.ServiceApi.Telegram;

public class ConversationStateStore
{
    private readonly ConcurrentDictionary<long, ConversationState> _states = new();
    private readonly TimeSpan _ttl = TimeSpan.FromMinutes(10);
    private readonly Timer _cleanupTimer;

    public ConversationStateStore()
    {
        _cleanupTimer = new Timer(CleanupExpired, null, TimeSpan.FromMinutes(2), TimeSpan.FromMinutes(2));
    }

    public ConversationState GetOrCreate(long chatId)
    {
        return _states.GetOrAdd(chatId, id => new ConversationState { ChatId = id });
    }

    public void Remove(long chatId) => _states.TryRemove(chatId, out _);

    private void CleanupExpired(object? state)
    {
        var cutoff = DateTime.UtcNow - _ttl;
        var expired = _states.Where(kv => kv.Value.LastActivity < cutoff).Select(kv => kv.Key).ToList();

        foreach (var chatId in expired)
            _states.TryRemove(chatId, out _);
    }
}
