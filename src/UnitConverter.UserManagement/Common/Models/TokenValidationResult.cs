using System;
using System.Collections.Generic;

namespace UnitConverter.UserManagement.Common.Models;

/// <summary>
/// Result of JWT token validation.
/// Contains extracted claims if the token is valid, or error details if invalid.
/// </summary>
public class TokenValidationResult
{
    /// <summary>
    /// Indicates whether the token is valid.
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// User ID extracted from the token claims.
    /// Only populated if IsValid is true.
    /// </summary>
    public long UserId { get; set; }

    /// <summary>
    /// Email address extracted from the token claims.
    /// Only populated if IsValid is true.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// List of roles assigned to the user from token claims.
    /// Only populated if IsValid is true.
    /// </summary>
    public List<string> Roles { get; set; } = new();

    /// <summary>
    /// Error message if token validation failed.
    /// Only populated if IsValid is false.
    /// </summary>
    public string ErrorMessage { get; set; } = string.Empty;
}
