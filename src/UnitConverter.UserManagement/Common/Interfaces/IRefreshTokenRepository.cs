using UnitConverter.UserManagement.Core.Domain.Entities;

namespace UnitConverter.UserManagement.Common.Interfaces;

/// <summary>
/// Persistence for issued refresh tokens.
/// </summary>
public interface IRefreshTokenRepository
{
    /// <summary>Stores a new refresh token.</summary>
    Task AddAsync(RefreshToken token, CancellationToken cancellationToken = default);

    /// <summary>Loads a refresh token by its opaque value.</summary>
    Task<RefreshToken?> GetByTokenValueAsync(string tokenValue, CancellationToken cancellationToken = default);

    /// <summary>Persists pending changes.</summary>
    Task SaveAsync(CancellationToken cancellationToken = default);
}
