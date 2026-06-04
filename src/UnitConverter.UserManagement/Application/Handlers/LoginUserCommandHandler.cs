using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using UnitConverter.UserManagement.Common.Interfaces;
using UnitConverter.UserManagement.Common.Models;
using UnitConverter.UserManagement.Core.Domain.Entities;
using UnitConverter.UserManagement.Application.Constants;
using UnitConverter.UserManagement.Core.Domain.ValueObjects;

namespace UnitConverter.UserManagement.Application.Handlers;

public sealed class LoginUserCommandHandler
{
    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenGenerator _tokenGenerator;
    private readonly JwtSettings _jwtSettings;
    private readonly IValidator<LoginRequest> _validator;
    private readonly ILogger<LoginUserCommandHandler> _logger;

    public LoginUserCommandHandler(
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IPasswordHasher passwordHasher,
        ITokenGenerator tokenGenerator,
        IOptions<JwtSettings> jwtSettings,
        IValidator<LoginRequest> validator,
        ILogger<LoginUserCommandHandler> logger)
    {
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _passwordHasher = passwordHasher;
        _tokenGenerator = tokenGenerator;
        _jwtSettings = jwtSettings.Value;
        _validator = validator;
        _logger = logger;
    }

    public async Task<TokenResponse> HandleAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        var email = Email.Create(request.Email);
        var user = await _userRepository.GetByEmailAsync(email);
        if (user is null || !user.IsActive ||
            !_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
        {
            _logger.LogWarning("Failed login attempt for email {Email}", email.Value);
            throw new UnauthorizedAccessException(AuthSecurityMessages.InvalidEmailOrPassword);
        }

        var userWithRoles = await _userRepository.GetWithRolesAsync(user.Id) ?? user;
        var roleNames = userWithRoles.Roles.Select(r => r.Name).ToArray();
        var accessToken = _tokenGenerator.GenerateAccessToken(userWithRoles, roleNames);
        var jwtId = ReadJwtId(accessToken);
        var refreshTokenValue = _tokenGenerator.GenerateRefreshToken();
        var expiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpiryDays);

        var refreshToken = RefreshToken.Create(
            id: 0,
            userId: user.Id,
            jwtId: jwtId,
            tokenValue: refreshTokenValue,
            expiresAt: expiresAt);

        await _refreshTokenRepository.AddAsync(refreshToken, cancellationToken);
        await _refreshTokenRepository.SaveAsync(cancellationToken);

        var issuedAt = DateTime.UtcNow;
        return new TokenResponse(
            accessToken,
            refreshTokenValue,
            _jwtSettings.AccessTokenExpiryMinutes * 60,
            issuedAt);
    }

    private static string ReadJwtId(string accessToken)
    {
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(accessToken);
        return jwt.Id ?? Guid.NewGuid().ToString("N");
    }
}
