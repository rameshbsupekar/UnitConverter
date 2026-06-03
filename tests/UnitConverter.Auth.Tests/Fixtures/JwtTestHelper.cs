using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;

namespace UnitConverter.Auth.Tests.Fixtures;

/// <summary>
/// Helper class for JWT token assertions and inspection.
/// Provides utilities for extracting and verifying JWT claim content without validation.
/// Used throughout JWT tests to assert token structure and content.
/// </summary>
public static class JwtTestHelper
{
    /// <summary>
    /// Extracts and counts the total number of claims in a JWT token.
    /// </summary>
    /// <param name="token">The JWT token string.</param>
    /// <returns>Count of claims in the token payload, or 0 if token is invalid.</returns>
    public static int GetClaimCount(string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);
            return jwtToken.Claims.Count();
        }
        catch
        {
            return 0;
        }
    }

    /// <summary>
    /// Checks if a claim of a specific type exists in the token.
    /// </summary>
    /// <param name="token">The JWT token string.</param>
    /// <param name="claimType">The claim type to search for (e.g., ClaimTypes.Email).</param>
    /// <returns>True if a claim of the type exists; otherwise false.</returns>
    public static bool HasClaim(string token, string claimType)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);
            return jwtToken.Claims.Any(c => c.Type == claimType);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Extracts the value of a claim by type.
    /// </summary>
    /// <param name="token">The JWT token string.</param>
    /// <param name="claimType">The claim type to extract (e.g., ClaimTypes.Email).</param>
    /// <returns>The claim value as a string, or empty string if not found or token is invalid.</returns>
    public static string GetClaimValue(string token, string claimType)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);
            return jwtToken.Claims.FirstOrDefault(c => c.Type == claimType)?.Value ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    /// <summary>
    /// Extracts all claim values for a specific claim type (useful for multi-valued claims like roles).
    /// </summary>
    /// <param name="token">The JWT token string.</param>
    /// <param name="claimType">The claim type to extract (e.g., ClaimTypes.Role).</param>
    /// <returns>List of claim values, or empty list if none found or token is invalid.</returns>
    public static List<string> GetClaimValues(string token, string claimType)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);
            return jwtToken.Claims
                .Where(c => c.Type == claimType)
                .Select(c => c.Value)
                .ToList();
        }
        catch
        {
            return new List<string>();
        }
    }

    /// <summary>
    /// Extracts the expiration time from a JWT token.
    /// </summary>
    /// <param name="token">The JWT token string.</param>
    /// <returns>DateTime of token expiration in UTC, or DateTime.MinValue if not found or token is invalid.</returns>
    public static DateTime GetExpirationTime(string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);
            var expClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Exp)?.Value;
            
            if (expClaim != null && long.TryParse(expClaim, out var exp))
            {
                // JWT exp is in Unix time (seconds since epoch)
                return DateTimeOffset.FromUnixTimeSeconds(exp).UtcDateTime;
            }

            return jwtToken.ValidTo;
        }
        catch
        {
            return DateTime.MinValue;
        }
    }

    /// <summary>
    /// Verifies that a token has a valid JWT format (3 parts separated by dots).
    /// </summary>
    /// <param name="token">The token string to verify.</param>
    /// <returns>True if token has valid JWT format; otherwise false.</returns>
    public static bool HasValidJwtFormat(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return false;

        var parts = token.Split('.');
        return parts.Length == 3;
    }
}
