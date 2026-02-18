using Microsoft.EntityFrameworkCore;
using Jullius.Data.Context;

namespace Jullius.ServiceApi.Configuration;

/// <summary>
/// Extensions para configuração de banco de dados e Entity Framework
/// </summary>
public static class DatabaseExtensions
{
    /// <summary>
    /// Configura o Entity Framework com PostgreSQL
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="connectionString">String de conexão do banco</param>
    /// <returns>Service collection para chaining</returns>
    public static IServiceCollection AddDatabaseConfiguration(
        this IServiceCollection services, 
        string connectionString)
    {
        services.AddDbContext<JulliusDbContext>(options =>
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorCodesToAdd: null);
                
                npgsqlOptions.CommandTimeout(60);
            }));

        return services;
    }
    
    /// <summary>
    /// Configura health checks para monitoramento do banco de dados
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <returns>Service collection para chaining</returns>
    public static IServiceCollection AddDatabaseHealthChecks(this IServiceCollection services)
    {
        services.AddHealthChecks();
        return services;
    }
} 