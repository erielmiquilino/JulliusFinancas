using System.ComponentModel.DataAnnotations;

namespace Jullius.Domain.Domain.Entities;

public class User
{
    [Key]
    public Guid Id { get; private set; }
    public string Email { get; private set; }
    public string PasswordHash { get; private set; }
    public string Name { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    // Navigation properties
    public ICollection<RefreshToken> RefreshTokens { get; private set; } = new List<RefreshToken>();
    public ICollection<PasswordResetToken> PasswordResetTokens { get; private set; } = new List<PasswordResetToken>();

    public User(string email, string passwordHash, string name)
    {
        Id = Guid.NewGuid();
        Email = email.ToLowerInvariant();
        PasswordHash = passwordHash;
        Name = name;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;

        Validate();
    }

    // For Entity Framework
    private User() { }

    private void Validate()
    {
        if (string.IsNullOrWhiteSpace(Email))
            throw new ArgumentException("Email cannot be empty");

        if (string.IsNullOrWhiteSpace(PasswordHash))
            throw new ArgumentException("PasswordHash cannot be empty");

        if (string.IsNullOrWhiteSpace(Name))
            throw new ArgumentException("Name cannot be empty");
    }

    public void UpdatePassword(string newPasswordHash)
    {
        if (string.IsNullOrWhiteSpace(newPasswordHash))
            throw new ArgumentException("PasswordHash cannot be empty");

        PasswordHash = newPasswordHash;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateProfile(string name, string email)
    {
        Name = name;
        Email = email.ToLowerInvariant();
        UpdatedAt = DateTime.UtcNow;

        Validate();
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }
}
