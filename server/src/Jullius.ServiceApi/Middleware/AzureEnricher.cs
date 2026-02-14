using Serilog.Core;
using Serilog.Events;

namespace Jullius.ServiceApi.Middleware;

/// <summary>
/// Enricher customizado para adicionar informações específicas do Azure
/// </summary>
public class AzureEnricher : ILogEventEnricher
{
    private readonly string? _websiteName;
    private readonly string? _websiteInstanceId;
    private readonly string? _websiteResourceGroup;
    private readonly string? _region;
    private readonly bool _isDocker;

    public AzureEnricher()
    {
        // Azure Web App environment variables
        _websiteName = Environment.GetEnvironmentVariable("WEBSITE_SITE_NAME");
        _websiteInstanceId = Environment.GetEnvironmentVariable("WEBSITE_INSTANCE_ID");
        _websiteResourceGroup = Environment.GetEnvironmentVariable("WEBSITE_RESOURCE_GROUP");
        _region = Environment.GetEnvironmentVariable("WEBSITE_REGION_NAME");
        _isDocker = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true";
    }

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        // Add Azure-specific properties if available
        if (!string.IsNullOrEmpty(_websiteName))
        {
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("AzureWebSiteName", _websiteName));
        }

        if (!string.IsNullOrEmpty(_websiteInstanceId))
        {
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("AzureInstanceId", _websiteInstanceId));
        }

        if (!string.IsNullOrEmpty(_websiteResourceGroup))
        {
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("AzureResourceGroup", _websiteResourceGroup));
        }

        if (!string.IsNullOrEmpty(_region))
        {
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("AzureRegion", _region));
        }

        // Add deployment information
        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("IsDocker", _isDocker));
        
        // Add custom application version if available
        var version = GetType().Assembly.GetName().Version?.ToString() ?? "Unknown";
        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("ApplicationVersion", version));
    }
}
