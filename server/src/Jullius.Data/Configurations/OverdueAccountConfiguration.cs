using Jullius.Domain.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jullius.Data.Configurations;

public class OverdueAccountConfiguration : IEntityTypeConfiguration<OverdueAccount>
{
    public void Configure(EntityTypeBuilder<OverdueAccount> builder)
    {
        builder.ToTable("OverdueAccounts");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Description)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.CurrentDebtValue)
            .IsRequired()
            .HasColumnType("decimal(18,2)");

        builder.Property(x => x.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP(6)");
    }
}
