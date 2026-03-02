using Jullius.Domain.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
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
    public DbSet<User> Users { get; set; } = null!;
    public DbSet<RefreshToken> RefreshTokens { get; set; } = null!;
    public DbSet<PasswordResetToken> PasswordResetTokens { get; set; } = null!;

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
        modelBuilder.ApplyConfiguration(new UserConfiguration());
        modelBuilder.ApplyConfiguration(new RefreshTokenConfiguration());
        modelBuilder.ApplyConfiguration(new PasswordResetTokenConfiguration());

        // Garante que todos os DateTime sejam tratados como UTC ao ler/escrever no PostgreSQL
        ApplyUtcDateTimeConvention(modelBuilder);
    }

    /// <summary>
    /// Aplica um ValueConverter global para todas as propriedades DateTime e DateTime?,
    /// garantindo que o Kind seja sempre UTC antes de enviar ao Npgsql.
    /// </summary>
    private static void ApplyUtcDateTimeConvention(ModelBuilder modelBuilder)
    {
        var dateTimeConverter = new ValueConverter<DateTime, DateTime>(
            v => v.Kind == DateTimeKind.Utc ? v : DateTime.SpecifyKind(v, DateTimeKind.Utc),
            v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

        var nullableDateTimeConverter = new ValueConverter<DateTime?, DateTime?>(
            v => v.HasValue
                ? (v.Value.Kind == DateTimeKind.Utc ? v : DateTime.SpecifyKind(v.Value, DateTimeKind.Utc))
                : v,
            v => v.HasValue
                ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc)
                : v);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(DateTime))
                    property.SetValueConverter(dateTimeConverter);
                else if (property.ClrType == typeof(DateTime?))
                    property.SetValueConverter(nullableDateTimeConverter);
            }
        }
    }
}