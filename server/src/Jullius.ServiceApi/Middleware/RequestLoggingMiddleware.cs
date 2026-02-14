using Serilog;
using System.Diagnostics;
using System.Text;

namespace Jullius.ServiceApi.Middleware;

public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var correlationId = GetOrCreateCorrelationId(context);
        
        // Add correlation ID to response headers for tracing (only if response hasn't started)
        if (!context.Response.HasStarted)
        {
            context.Response.Headers["X-Correlation-ID"] = correlationId;
        }
        
        // Add correlation ID to log context for all subsequent logs
        using (Serilog.Context.LogContext.PushProperty("CorrelationId", correlationId))
        {
            // Log request start
            LogRequestStart(context, correlationId);

            // Enable request body reading if needed for logging
            context.Request.EnableBuffering();
        
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, 
                    "Request failed for {Method} {Path} with CorrelationId {CorrelationId}",
                    context.Request.Method,
                    context.Request.Path,
                    correlationId);
                throw;
            }
            finally
            {
                stopwatch.Stop();
                LogRequestComplete(context, correlationId, stopwatch.ElapsedMilliseconds);
            }
        }
    }

    private string GetOrCreateCorrelationId(HttpContext context)
    {
        // Try to get correlation ID from headers first
        if (context.Request.Headers.TryGetValue("X-Correlation-ID", out var correlationId))
        {
            return correlationId.FirstOrDefault() ?? Guid.NewGuid().ToString();
        }

        // Create new correlation ID
        var newCorrelationId = Guid.NewGuid().ToString();
        context.Items["CorrelationId"] = newCorrelationId;
        return newCorrelationId;
    }

    private void LogRequestStart(HttpContext context, string correlationId)
    {
        var request = context.Request;
        
        _logger.LogInformation(
            "Request started: {Method} {Path}{QueryString} from {RemoteIpAddress} with CorrelationId {CorrelationId}",
            request.Method,
            request.Path,
            request.QueryString,
            GetClientIpAddress(context),
            correlationId);

        // Log request headers (filtered)
        var relevantHeaders = request.Headers
            .Where(h => IsRelevantHeader(h.Key))
            .ToDictionary(h => h.Key, h => h.Value.ToString());

        if (relevantHeaders.Any())
        {
            _logger.LogDebug("Request headers: {@Headers} with CorrelationId {CorrelationId}",
                relevantHeaders, correlationId);
        }
    }

    private void LogRequestComplete(HttpContext context, string correlationId, long elapsedMilliseconds)
    {
        var request = context.Request;
        var response = context.Response;

        var logLevel = response.StatusCode >= 400 ? LogLevel.Warning : LogLevel.Information;

        _logger.Log(logLevel,
            "Request completed: {Method} {Path} responded {StatusCode} in {ElapsedMilliseconds}ms with CorrelationId {CorrelationId}",
            request.Method,
            request.Path,
            response.StatusCode,
            elapsedMilliseconds,
            correlationId);
    }

    private string GetClientIpAddress(HttpContext context)
    {
        // Check for forwarded headers first (common in Azure)
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            return forwardedFor.Split(',')[0].Trim();
        }

        var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp))
        {
            return realIp;
        }

        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    private bool IsRelevantHeader(string headerName)
    {
        var relevantHeaders = new[]
        {
            "User-Agent", "Authorization", "Content-Type", "Accept",
            "X-Correlation-ID", "X-Forwarded-For", "X-Real-IP"
        };

        return relevantHeaders.Contains(headerName, StringComparer.OrdinalIgnoreCase);
    }
}
