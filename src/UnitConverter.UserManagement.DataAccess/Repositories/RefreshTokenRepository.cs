using Microsoft.EntityFrameworkCore;
using UnitConverter.UserManagement.Common.Interfaces;
using UnitConverter.UserManagement.Core.Domain.Entities;
using UnitConverter.UserManagement.DataAccess.Data;

namespace UnitConverter.UserManagement.DataAccess.Repositories;

/// <inheritdoc />
public sealed class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly AuthDbContext _context;

    public RefreshTokenRepository(AuthDbContext context) =>
        _context = context ?? throw new ArgumentNullException(nameof(context));

    /// <inheritdoc />
    public async Task AddAsync(RefreshToken token, CancellationToken cancellationToken = default) =>
        await _context.RefreshTokens.AddAsync(token, cancellationToken);

    /// <inheritdoc />
    public async Task<RefreshToken?> GetByTokenValueAsync(string tokenValue, CancellationToken cancellationToken = default) =>
        await _context.RefreshTokens.FirstOrDefaultAsync(t => t.TokenValue == tokenValue, cancellationToken);

    /// <inheritdoc />
    public Task SaveAsync(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);
}
