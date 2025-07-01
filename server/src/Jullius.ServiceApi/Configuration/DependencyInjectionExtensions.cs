using Jullius.Domain.Domain.Repositories;
using Jullius.Data.Repositories;
using Jullius.ServiceApi.Application.Services;

namespace Jullius.ServiceApi.Configuration;

/// <summary>
/// Extensions para configuração de injeção de dependência
/// </summary>
public static class DependencyInjectionExtensions
{
    /// <summary>
    /// Registra todos os repositórios do domínio
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <returns>Service collection para chaining</returns>
    public static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        // Repositórios de dados
        services.AddScoped<IFinancialTransactionRepository, FinancialTransactionRepository>();
        services.AddScoped<ICardRepository, CardRepository>();
        services.AddScoped<ICardTransactionRepository, CardTransactionRepository>();

        return services;
    }

    /// <summary>
    /// Registra todos os serviços de aplicação
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <returns>Service collection para chaining</returns>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Serviços de domínio
        services.AddScoped<FinancialTransactionService>();
        services.AddScoped<CardService>();
        services.AddScoped<CardTransactionService>();

        return services;
    }

    /// <summary>
    /// Registra todas as dependências da aplicação
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <returns>Service collection para chaining</returns>
    public static IServiceCollection AddApplicationDependencies(this IServiceCollection services)
    {
        return services
            .AddRepositories()
            .AddApplicationServices();
    }
} 