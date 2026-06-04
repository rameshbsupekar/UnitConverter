using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using UnitConverter.Common.Contracts.Resilience;
using UnitConverter.UnitsDefinitions.Api.Services;
using UnitConverter.Common.Constants;
using UnitConverter.Common.Contracts.Paging;
using UnitConverter.Common.Security;
using UnitConverter.UnitsDefinitions.Contracts.Requests;
using UnitConverter.UnitsDefinitions.Contracts.Responses;
using UnitConverter.UnitsDefinitions.Contracts.Units;

namespace UnitConverter.UnitsDefinitions.Api.Controllers;

/// <summary>
/// Unit definition master data: CRUD and approval workflow.
/// Create and update: Admin, Employee, or Partner (non-Admin submissions require Admin approval).
/// Delete, approve, reject, and admin correction: Admin only.
/// </summary>
[ApiController]
[ApiVersion(AppApiVersions.V1)]
[Route(ApiRouteTemplates.VersionedApiPrefix + ApiRouteSegments.UnitDefinitions)]
[Authorize(Policy = AuthorizationPolicies.MasterDataCrud)]
[EnableRateLimiting(RateLimitPolicyNames.SlidingWindowByUser)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public sealed class UnitDefinitionsController : ControllerBase
{
    private readonly IUnitAdminService _admin;

    public UnitDefinitionsController(IUnitAdminService admin) =>
        _admin = admin ?? throw new ArgumentNullException(nameof(admin));

    [HttpGet(Name = ApiRouteNames.ListUnitDefinitions)]
    [ProducesResponseType(typeof(PagedResult<UnitDetailResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<UnitDetailResponse>>> ListAsync(
        [FromQuery] UnitCategory? category,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var result = await _admin.ListAsync(category, new PagedRequest(page, pageSize), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:int}", Name = ApiRouteNames.GetUnitDefinitionById)]
    [ProducesResponseType(typeof(UnitDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UnitDetailResponse>> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var unit = await _admin.GetByIdAsync(id, cancellationToken);
        return unit is null ? NotFound() : Ok(unit);
    }

    [HttpPost(Name = ApiRouteNames.CreateUnitDefinition)]
    [ProducesResponseType(typeof(UnitDetailResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UnitDetailResponse>> CreateAsync(
        [FromBody] CreateUnitRequest request,
        CancellationToken cancellationToken)
    {
        var created = await _admin.CreateAsync(request, cancellationToken);
        return Created(ApiV1Paths.UnitDefinitionById(created.Id), created);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(UnitDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UnitDetailResponse>> UpdateAsync(
        int id,
        [FromBody] UpdateUnitRequest request,
        CancellationToken cancellationToken) =>
        Ok(await _admin.UpdateAsync(id, request, cancellationToken));

    [HttpPut("{id:int}/" + ApiRouteSegments.Approve)]
    [Authorize(Policy = AuthorizationPolicies.MasterDataAdminApprove)]
    [ProducesResponseType(typeof(UnitDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UnitDetailResponse>> ApproveAsync(
        int id,
        CancellationToken cancellationToken) =>
        Ok(await _admin.ApproveAsync(id, cancellationToken));

    [HttpPut("{id:int}/" + ApiRouteSegments.Reject)]
    [Authorize(Policy = AuthorizationPolicies.MasterDataAdminApprove)]
    [ProducesResponseType(typeof(UnitDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UnitDetailResponse>> RejectAsync(
        int id,
        [FromBody] RejectUnitRequest request,
        CancellationToken cancellationToken) =>
        Ok(await _admin.RejectAsync(id, request, cancellationToken));

    /// <summary>
    /// Admin-only correction of a unit definition regardless of who submitted it.
    /// </summary>
    [HttpPut("{id:int}/" + ApiRouteSegments.AdminCorrection)]
    [Authorize(Policy = AuthorizationPolicies.MasterDataAdminOverride)]
    [ProducesResponseType(typeof(UnitDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<ActionResult<UnitDetailResponse>> AdminCorrectionAsync(
        int id,
        [FromBody] UpdateUnitRequest request,
        CancellationToken cancellationToken) =>
        UpdateAsync(id, request, cancellationToken);

    [HttpDelete("{id:int}")]
    [Authorize(Policy = AuthorizationPolicies.MasterDataAdminDelete)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        await _admin.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
