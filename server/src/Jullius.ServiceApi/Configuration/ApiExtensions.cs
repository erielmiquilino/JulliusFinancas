using Microsoft.AspNetCore.OData;
using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;
using Microsoft.OpenApi.Models;
using Jullius.Domain.Domain.Entities;
using System.Text.Json;

namespace Jullius.ServiceApi.Configuration;

/// <summary>
/// Extensions para configuração de APIs, OData e documentação
/// </summary>
public static class ApiExtensions
{
    /// <summary>
    /// Configura controllers com OData e JSON options
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <returns>Service collection para chaining</returns>
    public static IServiceCollection AddApiConfiguration(this IServiceCollection services)
    {
        services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            })
            .AddOData(options => options
                .Select()
                .Filter()
                .OrderBy()
                .SetMaxTop(100)
                .Count()
                .Expand()
                .AddRouteComponents("api", GetEdmModel()));

        return services;
    }

    /// <summary>
    /// Configura Swagger para documentação da API
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <returns>Service collection para chaining</returns>
    public static IServiceCollection AddSwaggerConfiguration(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo 
            { 
                Title = "Jullius Finanças API", 
                Version = "v1",
                Description = "API para gerenciamento de finanças pessoais",
                Contact = new OpenApiContact
                {
                    Name = "Jullius Finanças",
                    Email = "contato@julliusfinancas.com"
                }
            });
        });

        return services;
    }

    /// <summary>
    /// Configura CORS para permitir requisições cross-origin
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <returns>Service collection para chaining</returns>
    public static IServiceCollection AddCorsConfiguration(this IServiceCollection services)
    {
        services.AddCors(options =>
        {
            options.AddPolicy("AllowAll", policyBuilder =>
                policyBuilder.AllowAnyOrigin()
                       .AllowAnyMethod()
                       .AllowAnyHeader());
        });

        return services;
    }

    /// <summary>
    /// Configuração do modelo EDM para OData
    /// </summary>
    /// <returns>Modelo EDM configurado</returns>
    private static IEdmModel GetEdmModel()
    {
        var odataBuilder = new ODataConventionModelBuilder();
        
        // Configuração das entidades
        var financialTransactionType = odataBuilder.EntitySet<FinancialTransaction>("FinancialTransactions");
        financialTransactionType.EntityType.HasKey(e => e.Id);
        
        var cardType = odataBuilder.EntitySet<Card>("Cards");
        cardType.EntityType.HasKey(e => e.Id);
        
        var cardTransactionType = odataBuilder.EntitySet<CardTransaction>("CardTransactions");
        cardTransactionType.EntityType.HasKey(e => e.Id);
        
        return odataBuilder.GetEdmModel();
    }
} 