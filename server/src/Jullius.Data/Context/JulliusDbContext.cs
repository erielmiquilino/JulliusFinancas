using Jullius.Domain.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Jullius.Data.Configurations;

namespace Jullius.Data.Context;

public class JulliusDbContext(DbContextOptions<JulliusDbContext> options) : DbContext(options)
{
    public DbSet<FinancialTransaction> FinancialTransactions { get; set; } = null!;
    public DbSet<Card> Cards { get; set; } = null!;
    public DbSet<CardTransaction> CardTransactions { get; set; } = null!;
    public DbSet<CardDescriptionMapping> CardDescriptionMappings { get; set; } = null!;
    public DbSet<Category> Categories { get; set; } = null!;
    public DbSet<Budget> Budgets { get; set; } = null!;
    public DbSet<OverdueAccount> OverdueAccounts { get; set; } = null!;
    public DbSet<BotConfiguration> BotConfigurations { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.ApplyConfiguration(new FinancialTransactionConfiguration());
        modelBuilder.ApplyConfiguration(new CardConfiguration());
        modelBuilder.ApplyConfiguration(new CardTransactionConfiguration());
        modelBuilder.ApplyConfiguration(new CardDescriptionMappingConfiguration());
        modelBuilder.ApplyConfiguration(new CategoryConfiguration());
        modelBuilder.ApplyConfiguration(new BudgetConfiguration());
        modelBuilder.ApplyConfiguration(new OverdueAccountConfiguration());
        modelBuilder.ApplyConfiguration(new BotConfigurationConfiguration());
    }
}