using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using UnitConverter.UnitsDefinitions.Api.Infrastructure;
using UnitConverter.UnitsDefinitions.Api.Services;
using UnitConverter.Common.Constants;
using UnitConverter.Common.Contracts.Resilience;
using UnitConverter.Common.Contracts.Paging;
using UnitConverter.UnitsDefinitions.Contracts.Responses;
using UnitConverter.UnitsDefinitions.Contracts.Units;

namespace UnitConverter.UnitsDefinitions.Api.Controllers;

/// <summary>
/// Public read-only catalog: categories and approved units (no sign-in required).
/// </summary>
[ApiController]
[ApiVersion(AppApiVersions.V1)]
[Route(ApiRouteTemplates.VersionedApiPrefix + ApiRouteSegments.Catalog)]
[AllowAnonymous]
public sealed class CatalogController : ControllerBase
{
    private readonly ICatalogQueries _catalog;

    public CatalogController(ICatalogQueries catalog) => _catalog = catalog;

    /// <summary>Lists conversion categories (length, mass, temperature).</summary>
    [HttpGet(ApiRouteSegments.Categories)]
    [EnableRateLimiting(RateLimitPolicyNames.FixedWindowByIp)]
    [ProducesResponseType(typeof(IReadOnlyList<UnitCategoryResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<UnitCategoryResponse>>> GetCategoriesAsync(
        CancellationToken cancellationToken) =>
        Ok(await _catalog.GetCategoriesAsync(cancellationToken));

    /// <summary>
    /// Lists approved units for a category.
    /// Provide <c>category</c> (numeric id) and/or <c>categoryName</c> (wire name from GET categories).
    /// </summary>
    [HttpGet(ApiRouteSegments.CatalogUnits)]
    [EnableRateLimiting(RateLimitPolicyNames.FixedWindowByIp)]
    [ProducesResponseType(typeof(PagedResult<UnitCatalogItemResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<UnitCatalogItemResponse>>> GetUnitsAsync(
        [FromQuery] UnitCategory? category,
        [FromQuery] string? categoryName,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        if (!CatalogCategoryResolver.TryResolve(category, categoryName, out var resolved, out var error))
        {
            return BadRequest(new { error });
        }

        var result = await _catalog.GetUnitsByCategoryAsync(
            new GetUnitsByCategoryQuery(resolved),
            new PagedRequest(page, pageSize),
            cancellationToken);
        return Ok(result);
    }
}
