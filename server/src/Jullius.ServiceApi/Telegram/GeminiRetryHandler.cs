using System.Net;

namespace Jullius.ServiceApi.Telegram;

public sealed class GeminiRetryHandler : DelegatingHandler
{
    private static readonly TimeSpan[] RetryDelays =
    [
        TimeSpan.FromSeconds(2),
        TimeSpan.FromSeconds(5)
    ];

    private readonly ILogger<GeminiRetryHandler> _logger;

    public GeminiRetryHandler(ILogger<GeminiRetryHandler> logger)
    {
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt <= RetryDelays.Length; attempt++)
        {
            var requestClone = await CloneRequestAsync(request, cancellationToken);
            var response = await base.SendAsync(requestClone, cancellationToken);

            if (!ShouldRetry(response.StatusCode) || attempt == RetryDelays.Length)
                return response;

            var delay = RetryDelays[attempt];

            _logger.LogWarning(
                "Gemini returned transient status {StatusCode}. Retrying attempt {Attempt} in {DelaySeconds}s.",
                (int)response.StatusCode,
                attempt + 1,
                delay.TotalSeconds);

            response.Dispose();
            await Task.Delay(delay, cancellationToken);
        }

        throw new InvalidOperationException("Fluxo de retry do Gemini chegou a um estado inesperado.");
    }

    private static bool ShouldRetry(HttpStatusCode statusCode)
    {
        return statusCode is HttpStatusCode.ServiceUnavailable or HttpStatusCode.TooManyRequests or HttpStatusCode.GatewayTimeout;
    }

    private static async Task<HttpRequestMessage> CloneRequestAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var clone = new HttpRequestMessage(request.Method, request.RequestUri)
        {
            Version = request.Version,
            VersionPolicy = request.VersionPolicy
        };

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
}
