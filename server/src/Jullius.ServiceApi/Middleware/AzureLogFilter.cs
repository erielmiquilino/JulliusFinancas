using Serilog.Core;
using Serilog.Events;

namespace Jullius.ServiceApi.Middleware;

/// <summary>
/// Filtro de logs customizado para Azure Web Apps
/// </summary>
public class AzureLogFilter : ILogEventFilter
{
    private readonly bool _isProduction;
    private readonly bool _isDocker;

    public AzureLogFilter()
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
        _isProduction = environment == "Production";
        _isDocker = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true";
    }

    public bool IsEnabled(LogEvent logEvent)
    {
        // Always allow Critical and Error logs
        if (logEvent.Level >= LogEventLevel.Error)
            return true;

        // Filter out noisy OData warnings in production
        if (_isProduction && logEvent.Level == LogEventLevel.Warning)
        {
            var sourceContext = GetSourceContext(logEvent);
            if (sourceContext?.Contains("Microsoft.AspNetCore.OData") == true)
                return false;
        }

        // Filter out EF connection warnings in production for Docker
        if (_isProduction && _isDocker && logEvent.Level == LogEventLevel.Warning)
        {
            var sourceContext = GetSourceContext(logEvent);
            if (sourceContext?.Contains("Microsoft.EntityFrameworkCore") == true)
            {
                var messageTemplate = logEvent.MessageTemplate.Text;
                if (messageTemplate.Contains("A transient exception occurred") || 
                    messageTemplate.Contains("Database") && messageTemplate.Contains("not currently available"))
                {
                    return false; // Filter out expected Azure SQL transient errors
                }
            }
        }

        // Filter out health check logs in production
        if (_isProduction && logEvent.Level == LogEventLevel.Information)
        {
            var messageTemplate = logEvent.MessageTemplate.Text;
            if (messageTemplate.Contains("health check") || 
                messageTemplate.Contains("Health check"))
                return false;
        }

        return true;
    }

    private string? GetSourceContext(LogEvent logEvent)
    {
        if (logEvent.Properties.TryGetValue("SourceContext", out var sourceContextProperty) &&
            sourceContextProperty is ScalarValue scalarValue)
        {
            return scalarValue.Value?.ToString();
        }
        return null;
    }
}
