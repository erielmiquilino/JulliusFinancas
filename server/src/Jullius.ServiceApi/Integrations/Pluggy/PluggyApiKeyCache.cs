namespace Jullius.ServiceApi.Integrations.Pluggy;

/// <summary>
/// A apiKey da Pluggy expira em 2 horas e nunca deve ser persistida.
/// Guardamos só em memória, com margem de segurança sobre o prazo real.
/// </summary>
public sealed class PluggyApiKeyCache
{
    private static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(100);

    private readonly Lock _gate = new();
    private string? _apiKey;
    private DateTime _expiresAtUtc;

    public string? Get()
    {
        lock (_gate)
        {
            if (_apiKey is not null && DateTime.UtcNow < _expiresAtUtc)
                return _apiKey;

            _apiKey = null;
            return null;
        }
    }

    public void Set(string apiKey)
    {
        lock (_gate)
        {
            _apiKey = apiKey;
            _expiresAtUtc = DateTime.UtcNow.Add(Lifetime);
        }
    }

    public void Invalidate()
    {
        lock (_gate)
        {
            _apiKey = null;
            _expiresAtUtc = DateTime.MinValue;
        }
    }
}
