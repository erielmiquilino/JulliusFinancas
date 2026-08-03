using Jullius.Domain.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jullius.Data.Configurations;

public class ReconciliationItemConfiguration : IEntityTypeConfiguration<ReconciliationItem>
{
    public void Configure(EntityTypeBuilder<ReconciliationItem> builder)
    {
        builder.ToTable("ReconciliationItems");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.ExternalId)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(x => x.RawDescription)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(x => x.RawAmount)
            .IsRequired()
            .HasColumnType("decimal(18,2)");

        builder.Property(x => x.RawDate).IsRequired();

        builder.Property(x => x.RawCategory).HasMaxLength(120);
        builder.Property(x => x.CounterpartyName).HasMaxLength(200);
        builder.Property(x => x.CounterpartyDocument).HasMaxLength(32);
        builder.Property(x => x.PaymentMethod).HasMaxLength(32);

        builder.Property(x => x.ProposedDescription)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.ProposedCategoryId).IsRequired(false);
        builder.Property(x => x.ProposedType).IsRequired();

        builder.Property(x => x.Status)
            .IsRequired()
            .HasDefaultValue(ReconciliationItemStatus.Pending);

        builder.Property(x => x.ReviewFlag)
            .IsRequired()
            .HasDefaultValue(ReconciliationReviewFlag.None);

        builder.Property(x => x.MatchedItemId).IsRequired(false);
        builder.Property(x => x.CreatedTransactionId).IsRequired(false);

        builder.Property(x => x.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP(6)");

        builder.HasOne(x => x.Session)
            .WithMany()
            .HasForeignKey(x => x.SessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.BankAccount)
            .WithMany()
            .HasForeignKey(x => x.BankAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        // Chave de idempotência: uma transação da Pluggy nunca é importada duas vezes.
        builder.HasIndex(x => x.ExternalId)
            .HasDatabaseName("IX_ReconciliationItems_ExternalId")
            .IsUnique();

        builder.HasIndex(x => x.SessionId)
            .HasDatabaseName("IX_ReconciliationItems_SessionId");

        builder.HasIndex(x => x.Status)
            .HasDatabaseName("IX_ReconciliationItems_Status");
    }
}
