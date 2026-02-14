using System.ComponentModel.DataAnnotations;

namespace Jullius.Domain.Domain.Entities;

public class BotConfiguration
{
    [Key]
    public Guid Id { get; private set; }
    public string ConfigKey { get; private set; }
    public string EncryptedValue { get; private set; }
    public string? Description { get; private set; }
    public string? UserId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public BotConfiguration(string configKey, string encryptedValue, string? description = null, string? userId = null)
    {
        Id = Guid.NewGuid();
        ConfigKey = configKey;
        EncryptedValue = encryptedValue;
        Description = description;
        UserId = userId;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;

        Validate();
    }

    private BotConfiguration() { }

    private void Validate()
    {
        if (string.IsNullOrWhiteSpace(ConfigKey))
            throw new ArgumentException("ConfigKey cannot be empty");

        if (string.IsNullOrWhiteSpace(EncryptedValue))
            throw new ArgumentException("EncryptedValue cannot be empty");
    }

    public void UpdateValue(string encryptedValue, string? description = null)
    {
        EncryptedValue = encryptedValue;
        if (description != null)
            Description = description;
        UpdatedAt = DateTime.UtcNow;

        Validate();
    }
}
