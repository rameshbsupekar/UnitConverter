using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using UnitConverter.UnitsDefinitions.Api.Services;
using UnitConverter.Common.Constants;
using UnitConverter.Common.Contracts.Resilience;

namespace UnitConverter.UnitsDefinitions.Api.Controllers;

/// <summary>
/// Converts a numeric value from one unit to another (no sign-in required).
/// </summary>
[ApiController]
[ApiVersion(AppApiVersions.V1)]
[Route(ApiRouteTemplates.VersionedApiPrefix + ApiRouteSegments.UnitConversions)]
[AllowAnonymous]
public sealed class UnitConversionsController : ControllerBase
{
    private readonly IConvertUnitsHandler _handler;

    public UnitConversionsController(IConvertUnitsHandler handler) => _handler = handler;

    [HttpPost]
    [EnableRateLimiting(RateLimitPolicyNames.FixedWindowByIp)]
    [ProducesResponseType(typeof(ConversionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ConversionResponse>> ConvertAsync(
        [FromBody] ConvertUnitsRequest request,
        CancellationToken cancellationToken) =>
        Ok(await _handler.HandleAsync(request, cancellationToken));
}
