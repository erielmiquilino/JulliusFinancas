using Microsoft.EntityFrameworkCore;
using Jullius.Data.Context;

namespace Jullius.ServiceApi.Services;

/// <summary>
/// Serviço responsável por gerenciar migrations do banco de dados
/// Otimizado para MySQL
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
    /// Executa migrations em background com retry logic
    /// </summary>
    /// <returns>Task com resultado booleano (sucesso/falha)</returns>
    public async Task<bool> StartMigrationsAsync()
    {
        const int maxRetries = 10;
        const int baseDelaySeconds = 5; // Reduzido para fail-fast ou retry-fast
        
        IsRunning = true;
        StartTime = DateTime.UtcNow;
        
        _logger.LogInformation("🔄 Iniciando migração do banco de dados (MySQL). " +
            "Tentativas máximas: {MaxTentativas}, Hora de início: {HoraInicio}",
            maxRetries, StartTime);
        
        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            _logger.LogInformation("Tentativa {TentativaAtual} de {TotalTentativas} para executar migrations",
                attempt, maxRetries);
            try
            {
                await ExecuteMigrationAttempt(attempt, maxRetries);
                
                // Sucesso - finaliza o processo
                await CompleteMigration();
                return true;
            }
            catch (Exception ex) when (attempt < maxRetries)
            {
                await HandleRetryableError(ex, attempt, baseDelaySeconds);
            }
            catch (Exception ex)
            {
                // Última tentativa falhou
                await HandleFinalError(ex, maxRetries);
                return false;
            }
        }
        
        // Se chegou aqui, todas as tentativas falharam
        await HandleMaxRetriesExceeded(maxRetries);
        return false;
    }

    /// <summary>
    /// Executa uma tentativa de migration
    /// </summary>
    private async Task ExecuteMigrationAttempt(int attempt, int maxRetries)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<JulliusDbContext>();
        
        _logger.LogInformation("🔍 Tentativa {TentativaAtual}/{TotalTentativas} - Testando conexão com MySQL. " +
            "Tentando estabelecer conexão com o banco...", attempt, maxRetries);
        
        // Testa a conexão primeiro
        await dbContext.Database.CanConnectAsync();
        _logger.LogInformation("✅ Conexão estabelecida com sucesso! Banco MySQL está ativo");
        
        // Executa as migrations
        _logger.LogInformation("📊 Executando migrations do banco de dados...");
        
        var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync();
        var pendingList = pendingMigrations.ToList();
        
        if (pendingList.Any())
        {
            _logger.LogInformation("Encontradas {TotalMigrations} migrations pendentes: {@MigrationsList}",
                pendingList.Count, pendingList);
        }
        else
        {
            _logger.LogInformation("Nenhuma migration pendente encontrada. Banco está atualizado");
        }
        
        await dbContext.Database.MigrateAsync();
        
        _logger.LogInformation("✅ Migrations executadas com sucesso!");
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
        _logger.LogInformation("✅ Migração do banco de dados concluída com sucesso! " +
            "Duração total: {DuracaoMinutos:F2} minutos. " +
            "Início: {HoraInicio}, Fim: {HoraFim}",
            duration.TotalMinutes, StartTime, CompletedTime);
        
        await Task.CompletedTask;
    }

    /// <summary>
    /// Lida com erros que podem ser resolvidos com retry
    /// </summary>
    private async Task HandleRetryableError(Exception ex, int attempt, int baseDelaySeconds)
    {
        // Calcula delay exponencial
        var delay = TimeSpan.FromSeconds(baseDelaySeconds * Math.Pow(1.5, attempt - 1));
        
        _logger.LogWarning("⚠️ Tentativa {TentativaAtual} falhou. " +
            "Erro: {MensagemErro}. Tipo de exceção: {TipoExcecao}. " +
            "Próxima tentativa em {DelaySegundos} segundos...", 
            attempt, ex.Message, ex.GetType().Name, (int)delay.TotalSeconds);
        
        // Log detalhado para debugging se necessário
        _logger.LogDebug("Detalhes do erro na tentativa {TentativaAtual}: {StackTrace}",
            attempt, ex.StackTrace);
        
        await Task.Delay(delay);
    }

    /// <summary>
    /// Lida com erro final após todas as tentativas
    /// </summary>
    private async Task HandleFinalError(Exception ex, int maxRetries)
    {
        IsRunning = false;
        ErrorMessage = ex.Message;
        
        _logger.LogError(ex, "❌ Migração do banco de dados falhou após {MaxTentativas} tentativas. " +
            "Erro final: {MensagemErro}. Tipo de exceção: {TipoExcecao}. " +
            "Tempo total decorrido: {TempoDecorrido:F2} minutos", 
            maxRetries, ex.Message, ex.GetType().Name,
            (DateTime.UtcNow - StartTime!.Value).TotalMinutes);
        
        await Task.CompletedTask;
    }

    /// <summary>
    /// Lida com caso onde todas as tentativas foram esgotadas
    /// </summary>
    private async Task HandleMaxRetriesExceeded(int maxRetries)
    {
        IsRunning = false;
        ErrorMessage = "Falha após múltiplas tentativas de conexão com MySQL";
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
                    !string.IsNullOrEmpty(ErrorMessage) ? "error" : "pending"
        };
    }
} 