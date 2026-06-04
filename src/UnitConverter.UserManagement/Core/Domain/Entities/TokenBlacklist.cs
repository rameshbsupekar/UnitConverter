using System;

namespace UnitConverter.UserManagement.Core.Domain.Entities;

/// <summary>
/// TokenBlacklist entity - tracks revoked tokens for invalidation.
/// 
/// Responsibilities:
/// - Store revoked token JWTs (hashed for security)
/// - Support fast lookups to check token validity
/// - Prevent use of old/revoked tokens
/// 
/// Strategy: Store hash of token JTI for space efficiency and security.
/// </summary>
public class TokenBlacklist
{
    /// <summary>
    /// Unique identifier for this blacklist entry.
    /// </summary>
    public long Id { get; private set; }

    /// <summary>
    /// The JWT identifier (jti) that was revoked - stored as hash for security.
    /// </summary>
    public string JwtIdHash { get; private set; } = null!;

    /// <summary>
    /// When this token was blacklisted (UTC).
    /// </summary>
    public DateTime BlacklistedAt { get; private set; }

    /// <summary>
    /// When the token will expire. Used for cleanup (remove expired entries).
    /// </summary>
    public DateTime TokenExpiresAt { get; private set; }

    /// <summary>
    /// Private parameterless constructor for EF Core.
    /// </summary>
    private TokenBlacklist()
    {
    }

    /// <summary>
    /// Factory method to create a new TokenBlacklist entry.
    /// </summary>
    public static TokenBlacklist Create(
        long id,
        string jwtIdHash,
        DateTime tokenExpiresAt)
    {
        if (string.IsNullOrWhiteSpace(jwtIdHash))
            throw new ArgumentException("JWT ID hash cannot be empty.", nameof(jwtIdHash));

        if (tokenExpiresAt <= DateTime.UtcNow)
            throw new ArgumentException("Token expiration must be in the future.", nameof(tokenExpiresAt));

        return new TokenBlacklist
        {
            Id = id,
            JwtIdHash = jwtIdHash,
            TokenExpiresAt = tokenExpiresAt,
            BlacklistedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Check if this blacklist entry has expired (token can be cleaned up).
    /// </summary>
    public bool IsExpired()
    {
        return DateTime.UtcNow >= TokenExpiresAt;
    }

    /// <summary>
    /// Equality based on blacklist ID.
    /// </summary>
    public override bool Equals(object? obj)
    {
        if (obj is not TokenBlacklist other)
            return false;

        return Id == other.Id;
    }

    /// <summary>
    /// Hash code based on blacklist ID.
    /// </summary>
    public override int GetHashCode() => Id.GetHashCode();
}
