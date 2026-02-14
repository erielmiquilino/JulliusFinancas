using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Jullius.Domain.Domain.Entities;

namespace Jullius.Data.Configurations;

public class CardDescriptionMappingConfiguration : IEntityTypeConfiguration<CardDescriptionMapping>
{
    public void Configure(EntityTypeBuilder<CardDescriptionMapping> builder)
    {
        builder.ToTable("CardDescriptionMappings");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.OriginalDescription)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(x => x.MappedDescription)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.CardId)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .IsRequired();

        // Relacionamento com Card
        builder.HasOne(x => x.Card)
            .WithMany()
            .HasForeignKey(x => x.CardId)
            .OnDelete(DeleteBehavior.Cascade);

        // Índice composto para performance na busca por OriginalDescription + CardId
        builder.HasIndex(x => new { x.OriginalDescription, x.CardId })
            .HasDatabaseName("IX_CardDescriptionMappings_OriginalDescription_CardId")
            .IsUnique();

        // Índice por CardId para consultas por cartão
        builder.HasIndex(x => x.CardId)
            .HasDatabaseName("IX_CardDescriptionMappings_CardId");
    }
}
