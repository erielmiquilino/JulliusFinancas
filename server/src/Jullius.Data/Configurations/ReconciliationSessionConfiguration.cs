using Jullius.Domain.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jullius.Data.Configurations;

public class ReconciliationSessionConfiguration : IEntityTypeConfiguration<ReconciliationSession>
{
    public void Configure(EntityTypeBuilder<ReconciliationSession> builder)
    {
        builder.ToTable("ReconciliationSessions");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.PeriodFrom).IsRequired();
        builder.Property(x => x.PeriodTo).IsRequired();

        builder.Property(x => x.Status)
            .IsRequired()
            .HasDefaultValue(ReconciliationSessionStatus.Draft);

        builder.Property(x => x.StartedAt)
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP(6)");

        builder.Property(x => x.ClosedAt)
            .IsRequired(false);

        builder.HasIndex(x => x.Status)
            .HasDatabaseName("IX_ReconciliationSessions_Status");
    }
}
