using Jullius.ServiceApi.Configuration;
using Jullius.ServiceApi.Services;

namespace Jullius.ServiceApi;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        await ConfigureServices(builder.Services, builder.Configuration);

        var app = builder.Build();
        await ConfigurePipeline(app);
        await InitializeDatabase(app);
        await app.RunAsync();
    }

    private static async Task ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddApiConfiguration();
        services.AddSwaggerConfiguration();
        services.AddCorsConfiguration();
        var connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? throw new InvalidOperationException("String de conexão 'DefaultConnection' não encontrada");
        
        services.AddDatabaseConfiguration(connectionString);
        services.AddDatabaseHealthChecks();
        services.AddApplicationDependencies();
        services.AddSingleton<DatabaseMigrationService>();
        await Task.CompletedTask;
    }

    private static async Task ConfigurePipeline(WebApplication app)
    {
        app.UseSwaggerInDevelopment();
        app.UseStandardMiddleware();
        var migrationService = app.Services.GetRequiredService<DatabaseMigrationService>();
        app.MapMonitoringEndpoints(migrationService);
        
        await Task.CompletedTask;
    }

    private static async Task InitializeDatabase(WebApplication app)
    {
        var logger = app.Services.GetRequiredService<ILogger<Program>>();
        
        try
        {
            logger.LogInformation("sInicializando Jullius Finanças API...");
            
            var migrationService = app.Services.GetRequiredService<DatabaseMigrationService>();
            _ = migrationService.StartMigrationsAsync();
            
            logger.LogInformation("API inicializada com sucesso! Migrations executando em background...");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro durante a inicialização da aplicação: {ErrorMessage}", ex.Message);
            throw; 
        }
        
        await Task.CompletedTask;
    }
}
