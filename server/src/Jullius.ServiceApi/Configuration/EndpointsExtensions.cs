using Jullius.Data.Context;
using Jullius.ServiceApi.Services;

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
        // Endpoint de health check padrão
        app.MapHealthChecks("/health");

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
                    type = "Azure SQL Serverless",
                    retryEnabled = true,
                    note = "Banco serverless pode pausar quando inativo e demorar para despertar"
                }
            };
            
            return Results.Ok(response);
        })
        .WithName("StartupCheck")
        .WithTags("Monitoring")
        .WithSummary("Verifica o status de inicialização da aplicação")
        .WithDescription("Endpoint para verificar se a aplicação está rodando e o status das migrations");

        // Endpoint para despertar o banco de dados
        app.MapGet("/wakeup-db", async (JulliusDbContext dbContext, ILogger<Program> logger) =>
        {
            try
            {
                logger.LogInformation("🔍 Testando conexão com Azure SQL Serverless...");
                var startTime = DateTime.UtcNow;
                
                await dbContext.Database.CanConnectAsync();
                
                var duration = DateTime.UtcNow - startTime;
                logger.LogInformation("✅ Conexão estabelecida em {Duration}ms", duration.TotalMilliseconds);
                
                return Results.Ok(new {
                    status = "success",
                    message = "Conexão com banco estabelecida com sucesso",
                    duration = $"{duration.TotalMilliseconds:F0}ms",
                    timestamp = DateTime.UtcNow,
                    database = "Azure SQL Serverless"
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
        .WithName("WakeUpDatabase")
        .WithTags("Database")
        .WithSummary("Desperta o banco de dados Azure SQL Serverless")
        .WithDescription("Endpoint útil para despertar o banco serverless quando ele está em pausa");

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
        // Middleware de redirecionamento HTTPS
        app.UseHttpsRedirection();
        
        // CORS
        app.UseCors("AllowAll");
        
        // Autorização
        app.UseAuthorization();
        
        // Controllers
        app.MapControllers();

        return app;
    }
} 