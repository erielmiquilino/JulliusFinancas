using System.Net;
using System.Text.RegularExpressions;

namespace Jullius.ServiceApi.Integrations.Pluggy;

/// <summary>
/// Retry para falhas transitórias da API da Pluggy, no mesmo molde do GeminiRetryHandler.
/// Redige a chave de API nos logs.
/// </summary>
public sealed partial class PluggyRetryHandler : DelegatingHandler
{
    private static readonly TimeSpan[] DefaultRetryDelays =
    [
        TimeSpan.FromSeconds(2),
        TimeSpan.FromSeconds(5)
    ];

    private readonly ILogger<PluggyRetryHandler> _logger;
    private readonly TimeSpan[] _retryDelays;

    public PluggyRetryHandler(ILogger<PluggyRetryHandler> logger)
        : this(logger, null)
    {
    }

    public PluggyRetryHandler(ILogger<PluggyRetryHandler> logger, IEnumerable<TimeSpan>? retryDelays)
    {
        _logger = logger;
        _retryDelays = retryDelays?.ToArray() ?? DefaultRetryDelays;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt <= _retryDelays.Length; attempt++)
        {
            var requestClone = await CloneRequestAsync(request, cancellationToken);
            var response = await base.SendAsync(requestClone, cancellationToken);

            if (!ShouldRetry(response.StatusCode) || attempt == _retryDelays.Length)
                return response;

            var delay = _retryDelays[attempt];

            _logger.LogWarning(
                "Pluggy retornou status transitório {StatusCode} em {Uri}. Nova tentativa {Attempt} em {DelaySeconds}s.",
                (int)response.StatusCode,
                Sanitize(requestClone.RequestUri?.AbsoluteUri),
                attempt + 1,
                delay.TotalSeconds);

            response.Dispose();

            if (delay > TimeSpan.Zero)
                await Task.Delay(delay, cancellationToken);
        }

        throw new InvalidOperationException("Fluxo de retry da Pluggy chegou a um estado inesperado.");
    }

    private static bool ShouldRetry(HttpStatusCode statusCode)
    {
        return statusCode is HttpStatusCode.ServiceUnavailable
            or HttpStatusCode.TooManyRequests
            or HttpStatusCode.GatewayTimeout
            or HttpStatusCode.BadGateway;
    }

    private static string Sanitize(string? uri)
    {
        return uri is null ? "(unknown)" : ApiKeyRegex().Replace(uri, "$1***redacted***");
    }

    private static async Task<HttpRequestMessage> CloneRequestAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var clone = new HttpRequestMessage(request.Method, request.RequestUri)
        {
            Version = request.Version,
            VersionPolicy = request.VersionPolicy
        };

        foreach (var option in request.Options)
        {
            clone.Options.Set(new HttpRequestOptionsKey<object?>(option.Key), option.Value);
        }

        foreach (var header in request.Headers)
        {
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        if (request.Content is null)
            return clone;

        var contentBytes = await request.Content.ReadAsByteArrayAsync(cancellationToken);
        var contentClone = new ByteArrayContent(contentBytes);

        foreach (var header in request.Content.Headers)
        {
            contentClone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        clone.Content = contentClone;
        return clone;
    }

    [GeneratedRegex("([?&](?:key|api_key|apiKey)=)[^&]+", RegexOptions.IgnoreCase)]
    private static partial Regex ApiKeyRegex();
}
