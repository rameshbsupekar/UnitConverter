using System;

namespace UnitConverter.Auth.Common.Interfaces;

/// <summary>
/// Interface for encrypting and decrypting sensitive data.
/// Implementations should use DPAPI or similar secure encryption mechanisms.
/// </summary>
public interface IDataProtector
{
    /// <summary>
    /// Encrypts plaintext data.
    /// </summary>
    /// <param name="plaintext">The data to encrypt.</param>
    /// <returns>The encrypted (ciphertext) data.</returns>
    string Encrypt(string plaintext);

    /// <summary>
    /// Decrypts encrypted data.
    /// </summary>
    /// <param name="ciphertext">The encrypted data.</param>
    /// <returns>The decrypted plaintext data.</returns>
    string Decrypt(string ciphertext);
}
