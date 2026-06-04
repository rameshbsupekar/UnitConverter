using System;
using System.Collections.Generic;
using UnitConverter.UserManagement.Core.Domain.Entities;
using UnitConverter.UserManagement.Common.Models;

namespace UnitConverter.UserManagement.Common.Interfaces;

/// <summary>
/// Interface for JWT token generation and validation.
/// Handles creation of access and refresh tokens, as well as token validation.
/// </summary>
public interface ITokenGenerator
{
    /// <summary>
    /// Generates a new access token for a user.
    /// </summary>
    /// <param name="user">The user to create the token for.</param>
    /// <param name="roles">The roles assigned to the user.</param>
    /// <returns>A signed JWT access token.</returns>
    string GenerateAccessToken(User user, IEnumerable<string> roles);

    /// <summary>
    /// Generates a new refresh token.
    /// </summary>
    /// <returns>A refresh token string.</returns>
    string GenerateRefreshToken();

    /// <summary>
    /// Validates a JWT token and extracts claims.
    /// </summary>
    /// <param name="token">The JWT token to validate.</param>
    /// <returns>A TokenValidationResult containing validation status and claims.</returns>
    TokenValidationResult ValidateToken(string token);
}
