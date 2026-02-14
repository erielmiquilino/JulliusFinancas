using Jullius.Domain.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jullius.Data.Configurations;

public class BotConfigurationConfiguration : IEntityTypeConfiguration<BotConfiguration>
{
    public void Configure(EntityTypeBuilder<BotConfiguration> builder)
    {
        builder.ToTable("BotConfigurations");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.ConfigKey)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(x => x.ConfigKey)
            .IsUnique();

        builder.Property(x => x.EncryptedValue)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(x => x.Description)
            .HasMaxLength(500);

        builder.Property(x => x.UserId)
            .HasMaxLength(200);

        builder.Property(x => x.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP(6)");

        builder.Property(x => x.UpdatedAt)
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP(6)");
    }
}
