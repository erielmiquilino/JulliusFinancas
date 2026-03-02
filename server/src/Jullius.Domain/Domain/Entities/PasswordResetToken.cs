using System.ComponentModel.DataAnnotations;

namespace Jullius.Domain.Domain.Entities;

public class PasswordResetToken
{
    [Key]
    public Guid Id { get; private set; }
    public string Token { get; private set; }
    public Guid UserId { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public bool IsUsed { get; private set; }
    public DateTime CreatedAt { get; private set; }

    // Navigation property
    public User User { get; private set; } = null!;

    public PasswordResetToken(string token, Guid userId, DateTime expiresAt)
    {
        Id = Guid.NewGuid();
        Token = token;
        UserId = userId;
        ExpiresAt = expiresAt;
        IsUsed = false;
        CreatedAt = DateTime.UtcNow;

        Validate();
    }

    // For Entity Framework
    private PasswordResetToken() { }

    private void Validate()
    {
        if (string.IsNullOrWhiteSpace(Token))
            throw new ArgumentException("Token cannot be empty");

        if (UserId == Guid.Empty)
            throw new ArgumentException("UserId cannot be empty");
    }

    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool IsValid => !IsUsed && !IsExpired;

    public void MarkAsUsed()
    {
        IsUsed = true;
    }
}
