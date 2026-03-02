using Jullius.Domain.Domain.Repositories;
using Jullius.ServiceApi.Configuration;
using Jullius.ServiceApi.Services;
using Jullius.ServiceApi.Middleware;
using Microsoft.AspNetCore.DataProtection;
using Serilog;
using Serilog.Events;

namespace Jullius.ServiceApi;

public class Program
{
    public static async Task Main(string[] args)
    {
        // Configure Serilog first
        ConfigureSerilog();

        Log.Information("Starting Jullius Finanças API...");

        try
        {
            var builder = WebApplication.CreateBuilder(args);
            
            // Use Serilog as the logging provider
            builder.Host.UseSerilog();
            
            await ConfigureServices(builder.Services, builder.Configuration);

            // Add health checks with logging
            builder.Services.AddHealthChecksWithLogging(builder.Configuration);

            var app = builder.Build();
            await ConfigurePipeline(app);
            await InitializeDatabase(app);
            
            Log.Information("Jullius Finanças API started successfully");
            await app.RunAsync();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application terminated unexpectedly");
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    private static async Task ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddApiConfiguration();
        services.AddJwtAuthentication(configuration);
        services.AddSwaggerConfiguration();
        services.AddCorsConfiguration();
        var connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? throw new InvalidOperationException("String de conexão 'DefaultConnection' não encontrada");
        
        services.AddDatabaseConfiguration(connectionString);
        services.AddDatabaseHealthChecks();
        services.AddApplicationDependencies();
        services.AddSemanticKernelServices();
        ConfigureDataProtection(services, configuration);
        services.AddSingleton<DatabaseMigrationService>();
        services.AddHostedService<TelegramWebhookRegistrationService>();
        await Task.CompletedTask;
    }

    private static void ConfigureDataProtection(IServiceCollection services, IConfiguration configuration)
    {
        var appName = configuration["DataProtection:ApplicationName"] ?? "JulliusFinancasApi";
        var keysPath = configuration["DataProtection:KeysPath"];

        var dataProtectionBuilder = services
            .AddDataProtection()
            .SetApplicationName(appName);

        if (!string.IsNullOrWhiteSpace(keysPath))
        {
            var keysDirectory = Directory.CreateDirectory(keysPath);
            dataProtectionBuilder.PersistKeysToFileSystem(keysDirectory);

            Log.Information(
                "DataProtection configurado com persistência de chaves em {KeysPath} para {ApplicationName}",
                keysDirectory.FullName,
                appName);
            return;
        }

        Log.Warning(
            "DataProtection sem persistência explícita de chaves. Configure DataProtection:KeysPath para evitar perda de chave em reinícios/deploys.");
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
            logger.LogInformation("Inicializando Banco de Dados e Migrations...");
            
            var migrationService = app.Services.GetRequiredService<DatabaseMigrationService>();
            var success = await migrationService.StartMigrationsAsync();
            
            if (!success)
            {
                logger.LogCritical("FALHA CRÍTICA: Não foi possível aplicar as migrations do banco de dados. A aplicação será encerrada.");
                throw new Exception("Falha na inicialização do banco de dados.");
            }

            logger.LogInformation("Base de dados atualizada e pronta!");
            
            await SeedAdminUserAsync(app);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro fatal durante a inicialização do banco: {ErrorMessage}", ex.Message);
            throw; 
        }
    }

    private static async Task SeedAdminUserAsync(WebApplication app)
    {
        var logger = app.Services.GetRequiredService<ILogger<Program>>();
        var configuration = app.Services.GetRequiredService<IConfiguration>();

        var adminEmail = configuration["Admin:Email"];
        var adminPassword = configuration["Admin:Password"];
        var adminName = configuration["Admin:Name"] ?? "Administrador";

        if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPassword))
        {
            logger.LogWarning("Configuração Admin:Email ou Admin:Password não definida. Seed do usuário admin ignorado.");
            return;
        }

        using var scope = app.Services.CreateScope();
        var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();

        var existingUser = await userRepository.GetByEmailAsync(adminEmail);
        if (existingUser is not null)
        {
            logger.LogInformation("Usuário admin '{Email}' já existe. Seed ignorado.", adminEmail);
            return;
        }

        var passwordHash = BCrypt.Net.BCrypt.HashPassword(adminPassword, workFactor: 12);
        var adminUser = new Jullius.Domain.Domain.Entities.User(adminEmail, passwordHash, adminName);

        await userRepository.CreateAsync(adminUser);
        logger.LogInformation("Usuário admin '{Email}' criado com sucesso durante o seed.", adminEmail);
    }

    private static void ConfigureSerilog()
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
        var isDevelopment = environment == "Development";
        var isDocker = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true";

        var loggerConfig = new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
            .MinimumLevel.Override("Microsoft.AspNetCore.OData", LogEventLevel.Warning)
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .Filter.With<AzureLogFilter>()
            .Enrich.FromLogContext()
            .Enrich.WithEnvironmentName()
            .Enrich.WithMachineName()
            .Enrich.WithThreadId()
            .Enrich.WithCorrelationId()
            .Enrich.With<AzureEnricher>()
            .Enrich.WithProperty("Application", "JulliusFinancasApi")
            .Enrich.WithProperty("Platform", isDocker ? "Docker" : "Native");

        // Configure console output (always present for Docker)
        if (isDocker || isDevelopment)
        {
            // For Docker and Azure, use structured console output
            loggerConfig.WriteTo.Console(
                outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{Level:u3}] [{CorrelationId}] [{SourceContext}] {Message:lj} {Properties:j}{NewLine}{Exception}");
        }
        else
        {
            // For local development, use simple console output
            loggerConfig.WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}");
        }

        // File logging only for non-Docker environments
        if (!isDocker)
        {
            loggerConfig.WriteTo.File(
                path: "logs/jullius-api-.txt",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [{CorrelationId}] [{SourceContext}] {Message:lj} {Properties:j}{NewLine}{Exception}",
                shared: true,
                flushToDiskInterval: TimeSpan.FromSeconds(1));
        }

        // In development, enable more detailed logging
        if (isDevelopment)
        {
            loggerConfig.MinimumLevel.Debug()
                       .MinimumLevel.Override("Microsoft", LogEventLevel.Information);
        }

        Log.Logger = loggerConfig.CreateLogger();
    }
}
