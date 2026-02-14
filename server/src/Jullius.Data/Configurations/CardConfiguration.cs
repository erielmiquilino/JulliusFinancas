using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Jullius.Domain.Domain.Entities;

namespace Jullius.Data.Configurations;

public class CardConfiguration : IEntityTypeConfiguration<Card>
{
    public void Configure(EntityTypeBuilder<Card> builder)
    {
        builder.ToTable("Cards");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.IssuingBank)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.ClosingDay)
            .IsRequired();

        builder.Property(x => x.DueDay)
            .IsRequired();

        builder.Property(x => x.Limit)
            .IsRequired()
            .HasColumnType("decimal(18,2)");

        builder.Property(x => x.CurrentLimit)
            .IsRequired()
            .HasColumnType("decimal(18,2)");

        builder.Property(x => x.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP(6)");
    }
} 