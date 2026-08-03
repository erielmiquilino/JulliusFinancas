using Jullius.Domain.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jullius.Data.Configurations;

public class BankAccountConfiguration : IEntityTypeConfiguration<BankAccount>
{
    public void Configure(EntityTypeBuilder<BankAccount> builder)
    {
        builder.ToTable("BankAccounts");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(120);

        builder.Property(x => x.Institution)
            .IsRequired()
            .HasMaxLength(120);

        builder.Property(x => x.HolderName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.PluggyItemId)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(x => x.PluggyAccountId)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(x => x.OpeningBalance)
            .IsRequired()
            .HasColumnType("decimal(18,2)");

        builder.Property(x => x.HasOpeningBalance)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(x => x.OpeningBalanceTransactionId)
            .IsRequired(false);

        builder.Property(x => x.LastKnownBalance)
            .IsRequired()
            .HasColumnType("decimal(18,2)");

        builder.Property(x => x.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(x => x.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP(6)");

        builder.HasIndex(x => x.PluggyAccountId)
            .HasDatabaseName("IX_BankAccounts_PluggyAccountId")
            .IsUnique();
    }
}
