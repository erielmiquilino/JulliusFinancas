using Microsoft.EntityFrameworkCore;
using Jullius.Data.Context;

namespace Jullius.ServiceApi.Configuration;

/// <summary>
/// Extensions para configuração de banco de dados e Entity Framework
/// </summary>
public static class DatabaseExtensions
{
    /// <summary>
    /// Configura o Entity Framework com MySQL
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="connectionString">String de conexão do banco</param>
    /// <returns>Service collection para chaining</returns>
    public static IServiceCollection AddDatabaseConfiguration(
        this IServiceCollection services, 
        string connectionString)
    {
        var serverVersion = ServerVersion.AutoDetect(connectionString);

        services.AddDbContext<JulliusDbContext>(options =>
            options.UseMySql(connectionString, serverVersion, mySqlOptions =>
            {
                mySqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorNumbersToAdd: null);
                
                mySqlOptions.CommandTimeout(60);
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