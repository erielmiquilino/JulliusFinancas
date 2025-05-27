using Microsoft.EntityFrameworkCore;
using JulliusApi.Domain.Repositories;
using JulliusApi.Infrastructure.Data;
using JulliusApi.Infrastructure.Repositories;
using JulliusApi.Application.Services;
using JulliusApi.Application.DTOs;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register services
builder.Services.AddScoped<IFinancialTransactionRepository, FinancialTransactionRepository>();
builder.Services.AddScoped<FinancialTransactionService>();

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader());
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");

// Financial Transaction Endpoints
app.MapPost("/api/transactions", async (CreateFinancialTransactionRequest request, FinancialTransactionService service) =>
{
    try
    {
        var transaction = await service.CreateTransactionAsync(request);
        return Results.Created($"/api/transactions/{transaction.Id}", transaction);
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(ex.Message);
    }
})
.WithName("CreateTransaction")
.WithOpenApi();

app.MapGet("/api/transactions", async (FinancialTransactionService service) =>
{
    var transactions = await service.GetAllTransactionsAsync();
    return Results.Ok(transactions);
})
.WithName("GetAllTransactions")
.WithOpenApi();

app.MapGet("/api/transactions/{id}", async (Guid id, FinancialTransactionService service) =>
{
    var transaction = await service.GetTransactionByIdAsync(id);
    if (transaction == null)
        return Results.NotFound();
        
    return Results.Ok(transaction);
})
.WithName("GetTransactionById")
.WithOpenApi();

app.Run(); 