using System.Net;

namespace Jullius.ServiceApi.Telegram;

public sealed class GeminiRetryHandler : DelegatingHandler
{
    private const string PrimaryModel = "gemini-3-flash-preview";
    private const string FallbackModel = "gemini-2.5-flash";

    private static readonly TimeSpan[] DefaultRetryDelays =
    [
        TimeSpan.FromSeconds(2),
        TimeSpan.FromSeconds(5)
    ];

    private readonly ILogger<GeminiRetryHandler> _logger;
    private readonly TimeSpan[] _retryDelays;

    public GeminiRetryHandler(ILogger<GeminiRetryHandler> logger)
        : this(logger, null)
    {
    }

    public GeminiRetryHandler(ILogger<GeminiRetryHandler> logger, IEnumerable<TimeSpan>? retryDelays)
    {
        _logger = logger;
        _retryDelays = retryDelays?.ToArray() ?? DefaultRetryDelays;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return await SendWithResilienceAsync(request, allowModelFallback: true, cancellationToken);
    }

    private async Task<HttpResponseMessage> SendWithResilienceAsync(
        HttpRequestMessage requestTemplate,
        bool allowModelFallback,
        CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt <= _retryDelays.Length; attempt++)
        {
            var requestClone = await CloneRequestAsync(requestTemplate, cancellationToken);
            var response = await base.SendAsync(requestClone, cancellationToken);

            if (allowModelFallback && ShouldFallbackToStable(requestClone.RequestUri, response.StatusCode))
            {
                response.Dispose();

                var fallbackRequest = await CreateFallbackRequestAsync(requestTemplate, cancellationToken);
                _logger.LogWarning(
                    "Gemini primary model unavailable. Falling back from {PrimaryModel} to {FallbackModel}.",
                    PrimaryModel,
                    FallbackModel);

                return await SendWithResilienceAsync(fallbackRequest, allowModelFallback: false, cancellationToken);
            }

            if (!ShouldRetry(response.StatusCode) || attempt == _retryDelays.Length)
                return response;

            var delay = _retryDelays[attempt];

            _logger.LogWarning(
                "Gemini returned transient status {StatusCode} for model {ModelId}. Retrying attempt {Attempt} in {DelaySeconds}s.",
                (int)response.StatusCode,
                ExtractModelId(requestClone.RequestUri),
                attempt + 1,
                delay.TotalSeconds);

            response.Dispose();

            if (delay > TimeSpan.Zero)
                await Task.Delay(delay, cancellationToken);
        }

        throw new InvalidOperationException("Fluxo de retry do Gemini chegou a um estado inesperado.");
    }

    private static bool ShouldFallbackToStable(Uri? requestUri, HttpStatusCode statusCode)
    {
        return statusCode == HttpStatusCode.ServiceUnavailable && UsesModel(requestUri, PrimaryModel);
    }

    private static bool ShouldRetry(HttpStatusCode statusCode)
    {
        return statusCode is HttpStatusCode.ServiceUnavailable or HttpStatusCode.TooManyRequests or HttpStatusCode.GatewayTimeout;
    }

    private static bool UsesModel(Uri? requestUri, string modelId)
    {
        return requestUri is not null && requestUri.AbsoluteUri.Contains(modelId, StringComparison.Ordinal);
    }

    private async Task<HttpRequestMessage> CreateFallbackRequestAsync(
        HttpRequestMessage requestTemplate,
        CancellationToken cancellationToken)
    {
        var fallbackRequest = await CloneRequestAsync(requestTemplate, cancellationToken);
        fallbackRequest.RequestUri = ReplaceModelInUri(requestTemplate.RequestUri, FallbackModel);
        fallbackRequest.Headers.Remove("X-Jullius-Gemini-Fallback");
        fallbackRequest.Headers.TryAddWithoutValidation("X-Jullius-Gemini-Fallback", $"{PrimaryModel}->{FallbackModel}");
        return fallbackRequest;
    }

    private static Uri ReplaceModelInUri(Uri? requestUri, string targetModel)
    {
        if (requestUri is null)
            throw new InvalidOperationException("A URI da requisição do Gemini não estava disponível para aplicar fallback.");

        var replacedUri = requestUri.AbsoluteUri.Replace(PrimaryModel, targetModel, StringComparison.Ordinal);
        return new Uri(replacedUri, UriKind.Absolute);
    }

    private static string ExtractModelId(Uri? requestUri)
    {
        if (requestUri is null)
            return "(unknown)";

        const string modelSegment = "/models/";
        var absoluteUri = requestUri.AbsoluteUri;
        var modelStart = absoluteUri.IndexOf(modelSegment, StringComparison.Ordinal);

        if (modelStart < 0)
            return "(unknown)";

        modelStart += modelSegment.Length;
        var modelEnd = absoluteUri.IndexOf(':', modelStart);

        return modelEnd < 0
            ? "(unknown)"
            : absoluteUri[modelStart..modelEnd];
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
}
