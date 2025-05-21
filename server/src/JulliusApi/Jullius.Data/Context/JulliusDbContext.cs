using Julius.Domain.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Jullius.Data.Configurations;

namespace Jullius.Data.Context;

public class JulliusDbContext(DbContextOptions<JulliusDbContext> options) : DbContext(options)
{
    public DbSet<FinancialTransaction> FinancialTransactions { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.ApplyConfiguration(new FinancialTransactionConfiguration());
    }
}