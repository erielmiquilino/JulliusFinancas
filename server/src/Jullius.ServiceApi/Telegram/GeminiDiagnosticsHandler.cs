using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;

namespace Jullius.ServiceApi.Telegram;

public sealed partial class GeminiDiagnosticsHandler : DelegatingHandler
{
    private const int MaxLoggedContentLength = 3000;

    private readonly ILogger<GeminiDiagnosticsHandler> _logger;

    public GeminiDiagnosticsHandler(ILogger<GeminiDiagnosticsHandler> logger)
    {
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var response = await base.SendAsync(request, cancellationToken);
            stopwatch.Stop();

            if (!response.IsSuccessStatusCode)
            {
                var responseContent = response.Content is null
                    ? "(empty)"
                    : Truncate(await response.Content.ReadAsStringAsync(cancellationToken));

                _logger.LogWarning(
                    "Gemini HTTP returned non-success status. StatusCode: {StatusCode}. ReasonPhrase: {ReasonPhrase}. Method: {Method}. Uri: {Uri}. DurationMs: {DurationMs}. ResponseHeaders: {ResponseHeaders}. ResponseContent: {ResponseContent}",
                    (int)response.StatusCode,
                    response.ReasonPhrase ?? "(none)",
                    request.Method.Method,
                    SanitizeUri(request.RequestUri),
                    stopwatch.ElapsedMilliseconds,
                    FormatHeaders(response.Headers),
                    responseContent);
            }

            return response;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            stopwatch.Stop();

            _logger.LogError(
                ex,
                "Gemini HTTP request failed before receiving a successful response. Method: {Method}. Uri: {Uri}. DurationMs: {DurationMs}. RequestHeaders: {RequestHeaders}",
                request.Method.Method,
                SanitizeUri(request.RequestUri),
                stopwatch.ElapsedMilliseconds,
                FormatHeaders(request.Headers));

            throw;
        }
    }

    private static string SanitizeUri(Uri? uri)
    {
        if (uri is null)
            return "(not available)";

        var sanitized = SensitiveQueryStringRegex().Replace(uri.ToString(), "$1***redacted***");
        return Truncate(sanitized);
    }

    private static string FormatHeaders(HttpHeaders headers)
    {
        var values = headers.Select(header => $"{header.Key}={string.Join(",", header.Value)}");
        var text = string.Join("; ", values);
        return string.IsNullOrWhiteSpace(text) ? "(none)" : Truncate(text);
    }

    private static string Truncate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "(empty)";

        return value.Length <= MaxLoggedContentLength
            ? value
            : $"{value[..MaxLoggedContentLength]}...(truncated)";
    }

    [GeneratedRegex("([?&](?:key|api_key)=)[^&]+", RegexOptions.IgnoreCase)]
    private static partial Regex SensitiveQueryStringRegex();
}
