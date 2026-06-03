using System;
using BCrypt.Net;
using UnitConverter.Auth.Common.Interfaces;

namespace UnitConverter.Auth.Application.Services;

/// <summary>
/// Password hashing and verification service using bcrypt.
/// Provides secure password storage and verification with industry-standard bcrypt algorithm.
/// Work factor is set to 12 for good balance between security and performance.
/// </summary>
public class PasswordService : IPasswordHasher
{
    private const int WorkFactor = 12; // Bcrypt work factor - higher = more secure but slower

    /// <summary>
    /// Hashes a plaintext password using bcrypt with work factor 12.
    /// </summary>
    /// <param name="password">The plaintext password to hash.</param>
    /// <returns>A salted bcrypt hash of the password.</returns>
    /// <exception cref="ArgumentException">Thrown when password is null or empty.</exception>
    public string HashPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("Password cannot be null or empty", nameof(password));

        // BCrypt.Net-Core handles salt generation internally
        return BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);
    }

    /// <summary>
    /// Verifies a plaintext password against a bcrypt hash.
    /// Never throws exceptions; returns false on any error to prevent information leakage.
    /// </summary>
    /// <param name="password">The plaintext password to verify.</param>
    /// <param name="hash">The bcrypt hash to verify against.</param>
    /// <returns>True if the password matches the hash; otherwise false.</returns>
    public bool VerifyPassword(string password, string hash)
    {
        try
        {
            // Validate inputs
            if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(hash))
                return false;

            // Use bcrypt to verify - constant-time comparison prevents timing attacks
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }
        catch
        {
            // Never throw from password verification - fail gracefully
            // Exceptions could leak information about the hash format
            return false;
        }
    }
}
