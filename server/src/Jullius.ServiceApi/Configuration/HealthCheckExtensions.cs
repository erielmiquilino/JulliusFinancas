using Microsoft.Extensions.Diagnostics.HealthChecks;
using Serilog;

namespace Jullius.ServiceApi.Configuration;

/// <summary>
/// Extensões para configuração de health checks com logging integrado
/// </summary>
public static class HealthCheckExtensions
{
    /// <summary>
    /// Adiciona health checks com logging personalizado
    /// </summary>
    public static IServiceCollection AddHealthChecksWithLogging(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHealthChecks()
            .AddDbContextCheck<Jullius.Data.Context.JulliusDbContext>("database")
            .AddCheck("self", () => HealthCheckResult.Healthy("API is running"))
            .AddCheck<CustomHealthCheck>("application");

        return services;
    }

    /// <summary>
    /// Configura o endpoint de health checks
    /// </summary>
    public static IEndpointRouteBuilder MapHealthChecksWithLogging(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
        {
            ResponseWriter = async (context, report) =>
            {
                var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
                
                var response = new
                {
                    status = report.Status.ToString(),
                    checks = report.Entries.Select(x => new
                    {
                        name = x.Key,
                        status = x.Value.Status.ToString(),
                        exception = x.Value.Exception?.Message,
                        duration = x.Value.Duration
                    }),
                    totalDuration = report.TotalDuration
                };

                // Log health check results
                if (report.Status == HealthStatus.Healthy)
                {
                    logger.LogDebug("Health check passed: {@HealthCheckResult}", response);
                }
                else
                {
                    logger.LogWarning("Health check failed: {@HealthCheckResult}", response);
                }

                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(response));
            }
        });

        endpoints.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready")
        });

        endpoints.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
        {
            Predicate = _ => false
        });

        return endpoints;
    }
}

/// <summary>
/// Health check customizado para verificações específicas da aplicação
/// </summary>
public class CustomHealthCheck : IHealthCheck
{
    private readonly ILogger<CustomHealthCheck> _logger;

    public CustomHealthCheck(ILogger<CustomHealthCheck> logger)
    {
        _logger = logger;
    }

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            // Aqui você pode adicionar verificações específicas do seu sistema
            // Por exemplo: verificar se serviços externos estão disponíveis
            
            _logger.LogDebug("Executing custom health check");
            
            // Simulação de verificação
            var isHealthy = true; // Substitua pela sua lógica
            
            if (isHealthy)
            {
                return Task.FromResult(HealthCheckResult.Healthy("Application is healthy"));
            }
            
            return Task.FromResult(HealthCheckResult.Unhealthy("Application has issues"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during custom health check");
            return Task.FromResult(HealthCheckResult.Unhealthy("Health check failed", ex));
        }
    }
}
