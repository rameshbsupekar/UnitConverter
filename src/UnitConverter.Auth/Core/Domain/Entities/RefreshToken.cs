using System;
using UnitConverter.Auth.Core.Domain.ValueObjects;

namespace UnitConverter.Auth.Core.Domain.Entities;

/// <summary>
/// RefreshToken entity - represents a refresh token issued to a user.
/// Refresh tokens allow users to obtain new access tokens without re-authenticating.
/// 
/// Responsibilities:
/// - Store refresh token data (jti, expiration, revocation status)
/// - Track token usage and lifecycle
/// </summary>
public class RefreshToken
{
    /// <summary>
    /// Unique identifier for this refresh token.
    /// </summary>
    public long Id { get; private set; }

    /// <summary>
    /// The user who owns this refresh token.
    /// </summary>
    public UserId UserId { get; private set; } = null!;

    /// <summary>
    /// JWT identifier (jti claim) - uniquely identifies this token.
    /// Used for tracking and revocation.
    /// </summary>
    public string JwtId { get; private set; } = null!;

    /// <summary>
    /// Token string (encrypted in database, hashed in token blacklist).
    /// </summary>
    public string TokenValue { get; private set; } = null!;

    /// <summary>
    /// When this token expires (UTC).
    /// </summary>
    public DateTime ExpiresAt { get; private set; }

    /// <summary>
    /// When this token was created (UTC).
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Whether this token has been revoked.
    /// </summary>
    public bool IsRevoked { get; private set; }

    /// <summary>
    /// The user entity (navigation property).
    /// </summary>
    public User? User { get; private set; }

    /// <summary>
    /// Private parameterless constructor for EF Core.
    /// </summary>
    private RefreshToken()
    {
    }

    /// <summary>
    /// Factory method to create a new RefreshToken.
    /// </summary>
    public static RefreshToken Create(
        long id,
        UserId userId,
        string jwtId,
        string tokenValue,
        DateTime expiresAt)
    {
        if (string.IsNullOrWhiteSpace(jwtId))
            throw new ArgumentException("JWT ID cannot be empty.", nameof(jwtId));

        if (string.IsNullOrWhiteSpace(tokenValue))
            throw new ArgumentException("Token value cannot be empty.", nameof(tokenValue));

        if (expiresAt <= DateTime.UtcNow)
            throw new ArgumentException("Token expiration must be in the future.", nameof(expiresAt));

        return new RefreshToken
        {
            Id = id,
            UserId = userId,
            JwtId = jwtId,
            TokenValue = tokenValue,
            ExpiresAt = expiresAt,
            CreatedAt = DateTime.UtcNow,
            IsRevoked = false
        };
    }

    /// <summary>
    /// Revoke this refresh token.
    /// </summary>
    public void Revoke()
    {
        IsRevoked = true;
    }

    /// <summary>
    /// Check if this token is still valid (not expired and not revoked).
    /// </summary>
    public bool IsValid()
    {
        return !IsRevoked && DateTime.UtcNow < ExpiresAt;
    }

    /// <summary>
    /// Equality based on token ID.
    /// </summary>
    public override bool Equals(object? obj)
    {
        if (obj is not RefreshToken other)
            return false;

        return Id == other.Id;
    }

    /// <summary>
    /// Hash code based on token ID.
    /// </summary>
    public override int GetHashCode() => Id.GetHashCode();
}
