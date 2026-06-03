using System;

namespace UnitConverter.Auth.Common.Interfaces;

/// <summary>
/// Interface for secure password hashing and verification.
/// Implementations should use industry-standard algorithms (e.g., bcrypt, argon2).
/// </summary>
public interface IPasswordHasher
{
    /// <summary>
    /// Hashes a plaintext password using a secure algorithm.
    /// </summary>
    /// <param name="password">The plaintext password to hash.</param>
    /// <returns>A salted hash of the password.</returns>
    string HashPassword(string password);

    /// <summary>
    /// Verifies a plaintext password against a stored hash.
    /// </summary>
    /// <param name="password">The plaintext password to verify.</param>
    /// <param name="hash">The stored password hash.</param>
    /// <returns>True if the password matches the hash; otherwise false.</returns>
    bool VerifyPassword(string password, string hash);
}
