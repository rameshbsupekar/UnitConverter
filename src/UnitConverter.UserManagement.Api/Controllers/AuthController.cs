using System.Diagnostics.CodeAnalysis;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using UnitConverter.UserManagement.Application.Handlers;
using UnitConverter.Common.Attributes;
using UnitConverter.Common.Constants;
using UnitConverter.Common.Contracts.Resilience;

namespace UnitConverter.UserManagement.Api.Controllers;

/// <summary>
/// Identity and session endpoints. Catalog, conversion, and unit-definition APIs are on
/// <c>UnitConverter.UnitsDefinitions.Api</c> (separate host).
/// </summary>
[ApiController]
[ApiVersion(AppApiVersions.V1)]
[Route(ApiRouteTemplates.VersionedApiPrefix)]
[Audited]
[SuppressMessage(
    "Maintainability",
    "S6960:Controllers should not have too many responsibilities",
    Justification = "User registration and session lifecycle share the same host and audit policy.")]
public sealed class AuthController : ControllerBase
{
    private readonly RegisterUserCommandHandler _registerHandler;
    private readonly LoginUserCommandHandler _loginHandler;
    private readonly RefreshTokenCommandHandler _refreshHandler;

    public AuthController(
        RegisterUserCommandHandler registerHandler,
        LoginUserCommandHandler loginHandler,
        RefreshTokenCommandHandler refreshHandler)
    {
        _registerHandler = registerHandler;
        _loginHandler = loginHandler;
        _refreshHandler = refreshHandler;
    }

    /// <summary>Creates a new user account.</summary>
    /// <remarks>Returns <c>201</c> with <c>Location: /api/v1/users/{id}</c>. Use <c>Created</c> path, not <c>CreatedAtAction</c>, because there is no GET-by-id route.</remarks>
    [HttpPost(ApiRouteSegments.Users, Name = ApiRouteNames.RegisterUser)]
    [EnableRateLimiting(RateLimitPolicyNames.FixedWindowByIp)]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserResponse>> RegisterAsync(
        [FromBody] RegisterUserRequest request,
        CancellationToken cancellationToken)
    {
        var user = await _registerHandler.HandleAsync(request);
        return Created(ApiV1Paths.UserById(user.Id), user);
    }

    /// <summary>Creates a session and returns access and refresh tokens.</summary>
    [HttpPost(ApiRouteSegments.Sessions, Name = ApiRouteNames.CreateSession)]
    [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<TokenResponse>> LoginAsync(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken) =>
        Ok(await _loginHandler.HandleAsync(request, cancellationToken));

    /// <summary>Renews an access token using a valid refresh token.</summary>
    [HttpPost($"{ApiRouteSegments.Sessions}/{ApiRouteSegments.SessionRefresh}", Name = ApiRouteNames.RefreshSession)]
    [EnableRateLimiting(RateLimitPolicyNames.SlidingWindowByUser)]
    [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<TokenResponse>> RefreshAsync(
        [FromBody] RefreshTokenRequest request,
        CancellationToken cancellationToken) =>
        Ok(await _refreshHandler.HandleAsync(request, cancellationToken));
}
