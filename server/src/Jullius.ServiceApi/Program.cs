using Jullius.ServiceApi.Configuration;
using Jullius.ServiceApi.Services;

namespace Jullius.ServiceApi;

/// <summary>
/// Classe principal da aplicação Jullius Finanças API
/// Configurada seguindo padrões enterprise .NET com separação de responsabilidades
/// </summary>
public class Program
{
    /// <summary>
    /// Método principal de entrada da aplicação
    /// </summary>
    /// <param name="args">Argumentos da linha de comando</param>
    public static async Task Main(string[] args)
    {
        // Criação do builder da aplicação
        var builder = WebApplication.CreateBuilder(args);

        // ========================================
        // CONFIGURAÇÃO DE SERVIÇOS
        // ========================================
        
        await ConfigureServices(builder.Services, builder.Configuration);

        // ========================================
        // BUILD DA APLICAÇÃO
        // ========================================
        
        var app = builder.Build();
        
        // ========================================
        // CONFIGURAÇÃO DO PIPELINE
        // ========================================
        
        await ConfigurePipeline(app);

        // ========================================
        // INICIALIZAÇÃO DO BANCO DE DADOS
        // ========================================
        
        await InitializeDatabase(app);

        // ========================================
        // EXECUÇÃO DA APLICAÇÃO
        // ========================================
        
        await app.RunAsync();
    }

    /// <summary>
    /// Configura todos os serviços da aplicação
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configuration">Configuração da aplicação</param>
    private static async Task ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // Configurações de API (Controllers, OData, JSON)
        services.AddApiConfiguration();
        
        // Configuração do Swagger para documentação
        services.AddSwaggerConfiguration();
        
        // Configuração de CORS
        services.AddCorsConfiguration();
        
        // Configuração do banco de dados
        var connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? throw new InvalidOperationException("String de conexão 'DefaultConnection' não encontrada");
        
        services.AddDatabaseConfiguration(connectionString);
        
        // Configuração de health checks
        services.AddDatabaseHealthChecks();
        
        // Registro de dependências (Repositories e Services)
        services.AddApplicationDependencies();
        
        // Registro do serviço de migrations
        services.AddSingleton<DatabaseMigrationService>();
        
        await Task.CompletedTask;
    }

    /// <summary>
    /// Configura o pipeline de middleware da aplicação
    /// </summary>
    /// <param name="app">Web application</param>
    private static async Task ConfigurePipeline(WebApplication app)
    {
        // Configuração do Swagger apenas em desenvolvimento
        app.UseSwaggerInDevelopment();
        
        // Configuração de middleware padrão (HTTPS, CORS, Auth, Controllers)
        app.UseStandardMiddleware();
        
        // Configuração de endpoints de monitoramento
        var migrationService = app.Services.GetRequiredService<DatabaseMigrationService>();
        app.MapMonitoringEndpoints(migrationService);
        
        await Task.CompletedTask;
    }

    /// <summary>
    /// Inicializa o banco de dados executando migrations em background
    /// Otimizado para Azure SQL Database Serverless
    /// </summary>
    /// <param name="app">Web application</param>
    private static async Task InitializeDatabase(WebApplication app)
    {
        var logger = app.Services.GetRequiredService<ILogger<Program>>();
        
        try
        {
            logger.LogInformation("🚀 Inicializando Jullius Finanças API...");
            
            // Obtém o serviço de migrations e inicia o processo em background
            var migrationService = app.Services.GetRequiredService<DatabaseMigrationService>();
            
            // Inicia migrations de forma assíncrona (não bloqueia o startup)
            _ = migrationService.StartMigrationsAsync();
            
            logger.LogInformation("✅ API inicializada com sucesso! Migrations executando em background...");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "❌ Erro durante a inicialização da aplicação: {ErrorMessage}", ex.Message);
            throw; // Re-throw para falhar o startup se necessário
        }
        
        await Task.CompletedTask;
    }
}
