using Jullius.Domain.Domain.Repositories;
using Jullius.Data.Context;
using Jullius.Data.Repositories;
using Jullius.ServiceApi.Application.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;
using Microsoft.AspNetCore.OData;
using Jullius.Domain.Domain.Entities;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Configurar a porta para Azure
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.UseUrls($"http://*:{port}");

// Classe para rastrear o status das migrations
public class MigrationStatus
{
    public bool IsCompleted { get; set; } = false;
    public bool IsRunning { get; set; } = false;
    public string? ErrorMessage { get; set; } = null;
    public DateTime? StartTime { get; set; } = null;
    public DateTime? CompletedTime { get; set; } = null;
}

// Instância global do status das migrations
var migrationStatus = new MigrationStatus();

// Configuração do modelo EDM para OData
static IEdmModel GetEdmModel()
{
    var odataBuilder = new ODataConventionModelBuilder();
    odataBuilder.EntitySet<FinancialTransaction>("FinancialTransactions");
    var financialTransactionType = odataBuilder.EntityType<FinancialTransaction>();
    financialTransactionType.HasKey(e => e.Id);
    
    odataBuilder.EntitySet<Card>("Cards");
    var cardType = odataBuilder.EntityType<Card>();
    cardType.HasKey(e => e.Id);
    
    odataBuilder.EntitySet<CardTransaction>("CardTransactions");
    var cardTransactionType = odataBuilder.EntityType<CardTransaction>();
    cardTransactionType.HasKey(e => e.Id);
    
    return odataBuilder.GetEdmModel();
}

// Add services to the container
builder.Services.AddControllers()
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

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Jullius Finanças API", Version = "v1" });
});

// Configure DbContext
builder.Services.AddDbContext<JulliusDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Adicionar Health Checks
builder.Services.AddHealthChecks()
    .AddDbContext<JulliusDbContext>();

// Register services
builder.Services.AddScoped<IFinancialTransactionRepository, FinancialTransactionRepository>();
builder.Services.AddScoped<FinancialTransactionService>();
builder.Services.AddScoped<ICardRepository, CardRepository>();
builder.Services.AddScoped<CardService>();
builder.Services.AddScoped<ICardTransactionRepository, CardTransactionRepository>();
builder.Services.AddScoped<CardTransactionService>();

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policyBuilder =>
        policyBuilder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader());
});

var app = builder.Build();

// Atualiza o banco de dados automaticamente com tratamento de erro
// Executa migrations em background para não bloquear o startup
_ = Task.Run(async () =>
{
    try
    {
        migrationStatus.IsRunning = true;
        migrationStatus.StartTime = DateTime.UtcNow;
        
        // Aguarda um pouco para a aplicação estar disponível
        await Task.Delay(5000);
        
        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<JulliusDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        
        logger.LogInformation("Iniciando migração do banco de dados em background...");
        
        // Verifica se a conexão está disponível antes de tentar migrar
        var connectionString = dbContext.Database.GetConnectionString();
        logger.LogInformation("Testando conexão com o banco de dados...");
        
        await dbContext.Database.MigrateAsync();
        
        migrationStatus.IsCompleted = true;
        migrationStatus.IsRunning = false;
        migrationStatus.CompletedTime = DateTime.UtcNow;
        
        logger.LogInformation("Migração do banco de dados concluída com sucesso.");
    }
    catch (Exception ex)
    {
        var logger = app.Services.GetRequiredService<ILogger<Program>>();
        migrationStatus.IsRunning = false;
        migrationStatus.ErrorMessage = ex.Message;
        
        logger.LogError(ex, "Erro ao executar migração do banco de dados: {ErrorMessage}", ex.Message);
        // Migrations em background - não falhar a aplicação
    }
});

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Jullius Finanças API V1");
    });
}

// Adicionar endpoint de health check
app.MapHealthChecks("/health");

// Endpoint simples para verificar se a aplicação está rodando
app.MapGet("/startup", () => 
{
    var response = new { 
        status = "running", 
        timestamp = DateTime.UtcNow,
        message = "Aplicação iniciada com sucesso",
        migration = new {
            isCompleted = migrationStatus.IsCompleted,
            isRunning = migrationStatus.IsRunning,
            startTime = migrationStatus.StartTime,
            completedTime = migrationStatus.CompletedTime,
            errorMessage = migrationStatus.ErrorMessage,
            status = migrationStatus.IsCompleted ? "completed" : 
                    migrationStatus.IsRunning ? "running" : 
                    !string.IsNullOrEmpty(migrationStatus.ErrorMessage) ? "error" : "pending"
        }
    };
    
    return Results.Ok(response);
}).WithName("StartupCheck");

app.UseHttpsRedirection();
app.UseCors("AllowAll");

app.UseAuthorization();
app.MapControllers();

app.Run();
