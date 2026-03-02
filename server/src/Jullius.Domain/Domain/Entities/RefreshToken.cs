using System.ComponentModel.DataAnnotations;

namespace Jullius.Domain.Domain.Entities;

public class RefreshToken
{
    [Key]
    public Guid Id { get; private set; }
    public string Token { get; private set; }
    public Guid UserId { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public bool IsRevoked { get; private set; }
    public string? ReplacedByToken { get; private set; }

    // Navigation property
    public User User { get; private set; } = null!;

    public RefreshToken(string token, Guid userId, DateTime expiresAt)
    {
        Id = Guid.NewGuid();
        Token = token;
        UserId = userId;
        ExpiresAt = expiresAt;
        CreatedAt = DateTime.UtcNow;
        IsRevoked = false;

        Validate();
    }

    // For Entity Framework
    private RefreshToken() { }

    private void Validate()
    {
        if (string.IsNullOrWhiteSpace(Token))
            throw new ArgumentException("Token cannot be empty");

        if (UserId == Guid.Empty)
            throw new ArgumentException("UserId cannot be empty");
    }

    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool IsActive => !IsRevoked && !IsExpired;

    public void Revoke(string? replacedByToken = null)
    {
        IsRevoked = true;
        ReplacedByToken = replacedByToken;
    }
}
