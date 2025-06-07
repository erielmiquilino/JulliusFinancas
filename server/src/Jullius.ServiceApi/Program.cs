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
    
    return odataBuilder.GetEdmModel();
}

// Add services to the container
builder.Services.AddControllers()
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
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register services
builder.Services.AddScoped<IFinancialTransactionRepository, FinancialTransactionRepository>();
builder.Services.AddScoped<FinancialTransactionService>();
builder.Services.AddScoped<ICardRepository, CardRepository>();
builder.Services.AddScoped<CardService>();

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policyBuilder =>
        policyBuilder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader());
});

var app = builder.Build();

// Atualiza o banco de dados automaticamente
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<JulliusDbContext>();
    dbContext.Database.Migrate();
}

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Jullius Finanças API V1");
    });
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");

app.UseAuthorization();
app.MapControllers();

app.Run();
