using System.Security.Claims;
using UnitConverter.Common.Security;

namespace UnitConverter.UnitsDefinitions.Api.Security;

/// <inheritdoc />
public sealed class HttpContextCurrentUserAccessor : ICurrentUserAccessor
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpContextCurrentUserAccessor(IHttpContextAccessor httpContextAccessor) =>
        _httpContextAccessor = httpContextAccessor;

    /// <inheritdoc />
    public string Email
    {
        get
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user is null || user.Identity?.IsAuthenticated != true)
            {
                return "anonymous@local";
            }

            return user.FindFirstValue(ClaimTypes.Email)
                ?? user.FindFirstValue(ClaimTypes.Name)
                ?? user.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? "unknown@local";
        }
    }

    /// <inheritdoc />
    public bool IsAdmin =>
        _httpContextAccessor.HttpContext?.User.IsInRole(RoleNames.Admin) == true;
}
