using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Jullius.Domain.Domain.Entities;

namespace Jullius.Data.Configurations;

public class CardTransactionConfiguration : IEntityTypeConfiguration<CardTransaction>
{
    public void Configure(EntityTypeBuilder<CardTransaction> builder)
    {
        builder.ToTable("CardTransactions");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.CardId)
            .IsRequired();

        builder.Property(x => x.Description)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Amount)
            .IsRequired()
            .HasColumnType("decimal(18,2)");

        builder.Property(x => x.Date)
            .IsRequired();

        builder.Property(x => x.Installment)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(x => x.InvoiceYear)
            .IsRequired();

        builder.Property(x => x.InvoiceMonth)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        // Relacionamento com Card
        builder.HasOne(x => x.Card)
            .WithMany()
            .HasForeignKey(x => x.CardId)
            .OnDelete(DeleteBehavior.Cascade);
    }
} 