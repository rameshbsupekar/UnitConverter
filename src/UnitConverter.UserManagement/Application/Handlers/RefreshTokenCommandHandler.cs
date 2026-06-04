using System.Linq;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using UnitConverter.UserManagement.Common.Interfaces;
using UnitConverter.UserManagement.Application.Constants;
using UnitConverter.UserManagement.Common.Models;

namespace UnitConverter.UserManagement.Application.Handlers;

public sealed class RefreshTokenCommandHandler
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IUserRepository _userRepository;
    private readonly ITokenGenerator _tokenGenerator;
    private readonly JwtSettings _jwtSettings;
    private readonly IValidator<RefreshTokenRequest> _validator;
    private readonly ILogger<RefreshTokenCommandHandler> _logger;

    public RefreshTokenCommandHandler(
        IRefreshTokenRepository refreshTokenRepository,
        IUserRepository userRepository,
        ITokenGenerator tokenGenerator,
        IOptions<JwtSettings> jwtSettings,
        IValidator<RefreshTokenRequest> validator,
        ILogger<RefreshTokenCommandHandler> logger)
    {
        _refreshTokenRepository = refreshTokenRepository;
        _userRepository = userRepository;
        _tokenGenerator = tokenGenerator;
        _jwtSettings = jwtSettings.Value;
        _validator = validator;
        _logger = logger;
    }

    public async Task<TokenResponse> HandleAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        var stored = await _refreshTokenRepository.GetByTokenValueAsync(request.RefreshToken, cancellationToken);
        if (stored is null || !stored.IsValid())
        {
            _logger.LogWarning("Invalid or expired refresh token presented");
            throw new UnauthorizedAccessException(AuthSecurityMessages.InvalidOrExpiredRefreshToken);
        }

        var user = await _userRepository.GetWithRolesAsync(stored.UserId);
        if (user is null || !user.IsActive)
        {
            throw new UnauthorizedAccessException(AuthSecurityMessages.UserNotAvailable);
        }

        var roleNames = user.Roles.Select(r => r.Name).ToArray();
        var accessToken = _tokenGenerator.GenerateAccessToken(user, roleNames);
        var issuedAt = DateTime.UtcNow;

        return new TokenResponse(
            accessToken,
            request.RefreshToken,
            _jwtSettings.AccessTokenExpiryMinutes * 60,
            issuedAt);
    }
}
