namespace UnitConverter.UnitsDefinitions.Api.Security;

/// <summary>
/// Reads the authenticated user from the current HTTP request (JWT claims).
/// </summary>
public interface ICurrentUserAccessor
{
    /// <summary>Email (or fallback identifier) of the authenticated caller.</summary>
    string Email { get; }

    /// <summary>True when the caller has the Admin role.</summary>
    bool IsAdmin { get; }
}
