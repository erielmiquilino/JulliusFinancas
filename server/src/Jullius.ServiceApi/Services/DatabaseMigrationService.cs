using Microsoft.EntityFrameworkCore;
using Jullius.Data.Context;

namespace Jullius.ServiceApi.Services;

/// <summary>
/// Serviço responsável por gerenciar migrations do banco de dados
/// Otimizado para Azure SQL Database Serverless
/// </summary>
public class DatabaseMigrationService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DatabaseMigrationService> _logger;
    
    // Estado das migrations
    public bool IsCompleted { get; private set; } = false;
    public bool IsRunning { get; private set; } = false;
    public string? ErrorMessage { get; private set; }
    public DateTime? StartTime { get; private set; }
    public DateTime? CompletedTime { get; private set; }

    public DatabaseMigrationService(IServiceProvider serviceProvider, ILogger<DatabaseMigrationService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <summary>
    /// Executa migrations em background com retry logic para Azure SQL Serverless
    /// </summary>
    /// <returns>Task para execução assíncrona</returns>
    public Task StartMigrationsAsync()
    {
        return Task.Run(async () =>
        {
            const int maxRetries = 10;
            const int baseDelaySeconds = 30;
            
            IsRunning = true;
            StartTime = DateTime.UtcNow;
            
            _logger.LogInformation("🔄 Iniciando migração do banco de dados em background (Azure SQL Serverless)...");
            
            // Aguarda para que a aplicação esteja disponível
            await Task.Delay(TimeSpan.FromSeconds(10));
            
            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    await ExecuteMigrationAttempt(attempt, maxRetries);
                    
                    // Sucesso - finaliza o processo
                    await CompleteMigration();
                    return;
                }
                catch (Exception ex) when (attempt < maxRetries)
                {
                    await HandleRetryableError(ex, attempt, baseDelaySeconds);
                }
                catch (Exception ex)
                {
                    // Última tentativa falhou
                    await HandleFinalError(ex, maxRetries);
                    return;
                }
            }
            
            // Se chegou aqui, todas as tentativas falharam
            await HandleMaxRetriesExceeded(maxRetries);
        });
    }

    /// <summary>
    /// Executa uma tentativa de migration
    /// </summary>
    private async Task ExecuteMigrationAttempt(int attempt, int maxRetries)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<JulliusDbContext>();
        
        _logger.LogInformation("🔍 Tentativa {Attempt}/{MaxRetries} - Testando conexão com Azure SQL Serverless...", 
            attempt, maxRetries);
        
        // Testa a conexão primeiro (pode despertar o banco serverless)
        await dbContext.Database.CanConnectAsync();
        _logger.LogInformation("✅ Conexão estabelecida com sucesso!");
        
        // Executa as migrations
        _logger.LogInformation("📊 Executando migrations...");
        await dbContext.Database.MigrateAsync();
    }

    /// <summary>
    /// Finaliza o processo de migration com sucesso
    /// </summary>
    private async Task CompleteMigration()
    {
        IsCompleted = true;
        IsRunning = false;
        CompletedTime = DateTime.UtcNow;
        
        var duration = CompletedTime.Value - StartTime!.Value;
        _logger.LogInformation("✅ Migração concluída com sucesso! Duração: {Duration}", duration);
        
        await Task.CompletedTask;
    }

    /// <summary>
    /// Lida com erros que podem ser resolvidos com retry
    /// </summary>
    private async Task HandleRetryableError(Exception ex, int attempt, int baseDelaySeconds)
    {
        // Calcula delay exponencial para serverless
        var delay = TimeSpan.FromSeconds(baseDelaySeconds * Math.Pow(1.5, attempt - 1));
        
        _logger.LogWarning("⚠️ Tentativa {Attempt} falhou. Erro: {Error}. Tentando novamente em {Delay} segundos...", 
            attempt, ex.Message, (int)delay.TotalSeconds);
        
        await Task.Delay(delay);
    }

    /// <summary>
    /// Lida com erro final após todas as tentativas
    /// </summary>
    private async Task HandleFinalError(Exception ex, int maxRetries)
    {
        IsRunning = false;
        ErrorMessage = ex.Message;
        
        _logger.LogError(ex, "❌ Migração falhou após {MaxRetries} tentativas. Erro final: {ErrorMessage}", 
            maxRetries, ex.Message);
        
        await Task.CompletedTask;
    }

    /// <summary>
    /// Lida com caso onde todas as tentativas foram esgotadas
    /// </summary>
    private async Task HandleMaxRetriesExceeded(int maxRetries)
    {
        IsRunning = false;
        ErrorMessage = "Falha após múltiplas tentativas de conexão com Azure SQL Serverless";
        _logger.LogError("❌ Não foi possível conectar ao banco após {MaxRetries} tentativas", maxRetries);
        
        await Task.CompletedTask;
    }

    /// <summary>
    /// Obtém informações detalhadas sobre o status das migrations
    /// </summary>
    /// <returns>Objeto com informações de status</returns>
    public object GetMigrationStatus()
    {
        var currentTime = DateTime.UtcNow;
        
        return new
        {
            isCompleted = IsCompleted,
            isRunning = IsRunning,
            startTime = StartTime,
            completedTime = CompletedTime,
            duration = IsCompleted && StartTime.HasValue && CompletedTime.HasValue 
                ? (CompletedTime.Value - StartTime.Value).ToString(@"mm\:ss") 
                : null,
            errorMessage = ErrorMessage,
            status = IsCompleted ? "completed" : 
                    IsRunning ? "running" : 
                    !string.IsNullOrEmpty(ErrorMessage) ? "error" : "pending",
            info = IsRunning ? "Azure SQL Serverless pode demorar para despertar na primeira conexão" : null
        };
    }
} 