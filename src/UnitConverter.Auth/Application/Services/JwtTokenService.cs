using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using UnitConverter.Auth.Common.Interfaces;
using UnitConverter.Auth.Common.Models;
using UnitConverter.Auth.Core.Domain.Entities;

namespace UnitConverter.Auth.Application.Services;

/// <summary>
/// JWT token generation and validation service.
/// Handles creation of secure access tokens and refresh tokens, as well as token validation.
/// Uses HS256 symmetric signing with configurable expiry times.
/// </summary>
public class JwtTokenService : ITokenGenerator
{
    private readonly JwtSettings _settings;

    /// <summary>
    /// Initializes a new instance of the JwtTokenService.
    /// </summary>
    /// <param name="settings">JWT configuration settings including secret, issuer, audience, and expiry times.</param>
    /// <exception cref="ArgumentNullException">Thrown when settings is null.</exception>
    /// <exception cref="ArgumentException">Thrown when secret is less than 32 characters (HS256 requirement).</exception>
    public JwtTokenService(JwtSettings settings)
    {
        if (settings == null)
            throw new ArgumentNullException(nameof(settings), "JwtSettings cannot be null");

        if (string.IsNullOrWhiteSpace(settings.Secret) || settings.Secret.Length < 32)
            throw new ArgumentException("JWT secret must be at least 32 characters long (HS256 requirement)", nameof(settings));

        _settings = settings;
    }

    /// <summary>
    /// Generates a new access token for a user with their roles.
    /// </summary>
    /// <param name="user">The user to generate a token for.</param>
    /// <param name="roles">The roles assigned to the user.</param>
    /// <returns>A signed JWT access token as a string.</returns>
    /// <exception cref="ArgumentNullException">Thrown when user is null.</exception>
    public string GenerateAccessToken(User user, IEnumerable<string> roles)
    {
        if (user == null)
            throw new ArgumentNullException(nameof(user), "User cannot be null");

        var now = DateTime.UtcNow;
        var expiryTime = now.AddMinutes(_settings.AccessTokenExpiryMinutes);

        var claims = new List<Claim>
        {
            // User identity claims
            new Claim(ClaimTypes.NameIdentifier, user.Id.Value.ToString()),
            new Claim(ClaimTypes.Email, user.Email.Value),
            new Claim("org_id", user.OrganizationName),
            
            // Unique token ID to prevent token replay attacks
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        // Add role claims
        if (roles != null)
        {
            foreach (var role in roles)
            {
                if (!string.IsNullOrWhiteSpace(role))
                {
                    claims.Add(new Claim(ClaimTypes.Role, role));
                }
            }
        }

        var secretKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Secret));
        var signingCredentials = new SigningCredentials(secretKey, SecurityAlgorithms.HmacSha256);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = expiryTime,
            Issuer = _settings.Issuer,
            Audience = _settings.Audience,
            SigningCredentials = signingCredentials
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    /// <summary>
    /// Generates a new refresh token.
    /// Refresh tokens are opaque 32-byte random values encoded as base64.
    /// </summary>
    /// <returns>A refresh token as a base64-encoded string.</returns>
    public string GenerateRefreshToken()
    {
        var randomBytes = new byte[32];
        using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomBytes);
        }

        return Convert.ToBase64String(randomBytes);
    }

    /// <summary>
    /// Validates a JWT token and extracts claims.
    /// </summary>
    /// <param name="token">The JWT token to validate.</param>
    /// <returns>A TokenValidationResult containing validation status and extracted claims if valid, or error details if invalid.</returns>
    public Common.Models.TokenValidationResult ValidateToken(string token)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return new Common.Models.TokenValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "Token cannot be null or empty"
                };
            }

            var secretKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Secret));
            
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = secretKey,
                ValidateIssuer = true,
                ValidIssuer = _settings.Issuer,
                ValidateAudience = true,
                ValidAudience = _settings.Audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero // No tolerance for time skew
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out var validatedToken);

            // Extract claims from validated token
            var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var emailClaim = principal.FindFirst(ClaimTypes.Email)?.Value;
            var roleClaims = principal.FindAll(ClaimTypes.Role);

            if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out var userId))
            {
                return new Common.Models.TokenValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "Token does not contain a valid user ID claim"
                };
            }

            return new Common.Models.TokenValidationResult
            {
                IsValid = true,
                UserId = userId,
                Email = emailClaim ?? string.Empty,
                Roles = roleClaims?.Select(c => c.Value).ToList() ?? new List<string>()
            };
        }
        catch (SecurityTokenException ex)
        {
            return new Common.Models.TokenValidationResult
            {
                IsValid = false,
                ErrorMessage = $"Token validation failed: {ex.Message}"
            };
        }
        catch (Exception ex)
        {
            return new Common.Models.TokenValidationResult
            {
                IsValid = false,
                ErrorMessage = $"An error occurred during token validation: {ex.Message}"
            };
        }
    }
}
