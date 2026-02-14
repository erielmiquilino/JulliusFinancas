using System.ComponentModel.DataAnnotations;

namespace Jullius.Domain.Domain.Entities;

public class CardDescriptionMapping
{
    [Key]
    public Guid Id { get; private set; }
    public string OriginalDescription { get; private set; } = string.Empty;
    public string MappedDescription { get; private set; } = string.Empty;
    public Guid CardId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    // Navigation property
    public Card Card { get; private set; } = null!;

    public CardDescriptionMapping(string originalDescription, string mappedDescription, Guid cardId)
    {
        Id = Guid.NewGuid();
        OriginalDescription = originalDescription;
        MappedDescription = mappedDescription;
        CardId = cardId;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;

        Validate();
    }

    // For Entity Framework
    private CardDescriptionMapping() { }

    private void Validate()
    {
        if (string.IsNullOrWhiteSpace(OriginalDescription))
            throw new ArgumentException("OriginalDescription cannot be empty");

        if (string.IsNullOrWhiteSpace(MappedDescription))
            throw new ArgumentException("MappedDescription cannot be empty");

        if (CardId == Guid.Empty)
            throw new ArgumentException("CardId cannot be empty");

        if (OriginalDescription.Length > 500)
            throw new ArgumentException("OriginalDescription cannot exceed 500 characters");

        if (MappedDescription.Length > 200)
            throw new ArgumentException("MappedDescription cannot exceed 200 characters");
    }

    public void UpdateMappedDescription(string mappedDescription)
    {
        MappedDescription = mappedDescription;
        UpdatedAt = DateTime.UtcNow;

        if (string.IsNullOrWhiteSpace(MappedDescription))
            throw new ArgumentException("MappedDescription cannot be empty");

        if (MappedDescription.Length > 200)
            throw new ArgumentException("MappedDescription cannot exceed 200 characters");
    }
}
