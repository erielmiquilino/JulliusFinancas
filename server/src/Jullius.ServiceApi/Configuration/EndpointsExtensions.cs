using Jullius.Data.Context;
using Jullius.ServiceApi.Services;
using Jullius.ServiceApi.Middleware;

namespace Jullius.ServiceApi.Configuration;

/// <summary>
/// Extensions para configuração de endpoints customizados da aplicação
/// </summary>
public static class EndpointsExtensions
{
    /// <summary>
    /// Configura endpoints de monitoramento e status da aplicação
    /// </summary>
    /// <param name="app">Web application</param>
    /// <param name="migrationService">Serviço de migrations</param>
    /// <returns>Web application para chaining</returns>
    public static WebApplication MapMonitoringEndpoints(
        this WebApplication app, 
        DatabaseMigrationService migrationService)
    {
        // Endpoint de status da aplicação
        app.MapGet("/startup", () => 
        {
            var currentTime = DateTime.UtcNow;
            var startupDuration = migrationService.StartTime.HasValue 
                ? currentTime - migrationService.StartTime.Value 
                : (TimeSpan?)null;
            
            var response = new { 
                status = "running", 
                timestamp = currentTime,
                message = "Aplicação iniciada com sucesso",
                server = new {
                    environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development",
                    uptime = startupDuration?.ToString(@"hh\:mm\:ss") ?? "N/A"
                },
                migration = migrationService.GetMigrationStatus(),
                database = new {
                    type = "MySQL",
                    retryEnabled = true
                }
            };
            
            return Results.Ok(response);
        })
        .WithName("StartupCheck")
        .WithTags("Monitoring")
        .WithSummary("Verifica o status de inicialização da aplicação")
        .WithDescription("Endpoint para verificar se a aplicação está rodando e o status das migrations");

        // Endpoint para testar conexão com o banco
        app.MapGet("/ping-db", async (JulliusDbContext dbContext, ILogger<Program> logger) =>
        {
            try
            {
                logger.LogInformation("🔍 Testando conexão com MySQL...");
                var startTime = DateTime.UtcNow;
                
                await dbContext.Database.CanConnectAsync();
                
                var duration = DateTime.UtcNow - startTime;
                logger.LogInformation("✅ Conexão estabelecida em {Duration}ms", duration.TotalMilliseconds);
                
                return Results.Ok(new {
                    status = "success",
                    message = "Conexão com banco estabelecida com sucesso",
                    duration = $"{duration.TotalMilliseconds:F0}ms",
                    timestamp = DateTime.UtcNow,
                    database = "MySQL"
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "❌ Erro ao conectar com o banco: {Error}", ex.Message);
                
                return Results.Problem(
                    detail: ex.Message,
                    title: "Erro de conexão com banco",
                    statusCode: 503
                );
            }
        })
        .WithName("PingDatabase")
        .WithTags("Database")
        .WithSummary("Testa a conexão com o banco de dados MySQL")
        .WithDescription("Endpoint para verificar se a conexão com o MySQL está ativa");

        return app;
    }

    /// <summary>
    /// Configura middleware de desenvolvimento
    /// </summary>
    /// <param name="app">Web application</param>
    /// <returns>Web application para chaining</returns>
    public static WebApplication UseSwaggerInDevelopment(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Jullius Finanças API V1");
                c.RoutePrefix = string.Empty; // Serve swagger UI na raiz
            });
        }

        return app;
    }

    /// <summary>
    /// Configura middleware padrão da aplicação
    /// </summary>
    /// <param name="app">Web application</param>
    /// <returns>Web application para chaining</returns>
    public static WebApplication UseStandardMiddleware(this WebApplication app)
    {
        // Exception handling middleware (should be first)
        app.UseMiddleware<ExceptionHandlingMiddleware>();
        
        // Performance monitoring middleware (should be early in pipeline)
        app.UseMiddleware<PerformanceLoggingMiddleware>();
        
        // Request logging middleware (should be early in pipeline)
        app.UseMiddleware<RequestLoggingMiddleware>();
        
        // Middleware de redirecionamento HTTPS (apenas se não estiver em Docker ou HTTPS configurado)
        var isDocker = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true";
        if (!isDocker && app.Configuration.GetValue<bool>("UseHttpsRedirection", true))
        {
            app.UseHttpsRedirection();
        }
        
        // CORS
        app.UseCors("AllowAll");
        
        // Autenticação e Autorização
        app.UseAuthentication();
        app.UseAuthorization();
        
        // Controllers
        app.MapControllers();

        // Map health checks
        app.MapHealthChecksWithLogging();

        return app;
    }
} 