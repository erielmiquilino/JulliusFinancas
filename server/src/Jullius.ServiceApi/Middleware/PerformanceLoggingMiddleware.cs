using System.Diagnostics;

namespace Jullius.ServiceApi.Middleware;

/// <summary>
/// Middleware para monitoramento de performance das requisições
/// </summary>
public class PerformanceLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<PerformanceLoggingMiddleware> _logger;
    private readonly long _slowRequestThresholdMs;

    public PerformanceLoggingMiddleware(RequestDelegate next, ILogger<PerformanceLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
        _slowRequestThresholdMs = 5000; // 5 seconds threshold for slow requests
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var path = context.Request.Path.Value;
        var method = context.Request.Method;

        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();
            var elapsedMs = stopwatch.ElapsedMilliseconds;
            var statusCode = context.Response.StatusCode;

            // Log performance metrics
            if (elapsedMs > _slowRequestThresholdMs)
            {
                _logger.LogWarning(
                    "Slow request detected: {Method} {Path} took {ElapsedMs}ms and returned {StatusCode}",
                    method, path, elapsedMs, statusCode);
            }
            else if (elapsedMs > 1000) // Log requests over 1 second as information
            {
                _logger.LogInformation(
                    "Request performance: {Method} {Path} took {ElapsedMs}ms and returned {StatusCode}",
                    method, path, elapsedMs, statusCode);
            }

            // Add performance metrics to response headers for monitoring (only if response hasn't started)
            if (!context.Response.HasStarted)
            {
                context.Response.Headers["X-Response-Time-ms"] = elapsedMs.ToString();
            }
        }
    }
}
