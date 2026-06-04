using FluentValidation;
using UnitConverter.UnitsDefinitions.Api.Security;
using UnitConverter.Common.Constants;
using UnitConverter.Common.Contracts.Paging;
using UnitConverter.UnitsDefinitions.Contracts.Requests;
using UnitConverter.UnitsDefinitions.Contracts.Responses;
using UnitConverter.UnitsDefinitions.Catalog;
using UnitConverter.UnitsDefinitions.Contracts.Units;
using UnitConverter.UnitsDefinitions.Units;

namespace UnitConverter.UnitsDefinitions.Api.Services;

/// <inheritdoc />
public sealed class UnitAdminService : IUnitAdminService
{
    private readonly IUnitCatalogAdminRepository _repository;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly IValidator<CreateUnitRequest> _createValidator;
    private readonly IValidator<UpdateUnitRequest> _updateValidator;
    private readonly IValidator<RejectUnitRequest> _rejectValidator;

    public UnitAdminService(
        IUnitCatalogAdminRepository repository,
        ICurrentUserAccessor currentUser,
        IValidator<CreateUnitRequest> createValidator,
        IValidator<UpdateUnitRequest> updateValidator,
        IValidator<RejectUnitRequest> rejectValidator)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        _createValidator = createValidator ?? throw new ArgumentNullException(nameof(createValidator));
        _updateValidator = updateValidator ?? throw new ArgumentNullException(nameof(updateValidator));
        _rejectValidator = rejectValidator ?? throw new ArgumentNullException(nameof(rejectValidator));
    }

    /// <inheritdoc />
    public async Task<UnitDetailResponse> CreateAsync(
        CreateUnitRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        await ThrowIfInvalidAsync(_createValidator, request, cancellationToken);

        if (await _repository.ExistsBySymbolAsync(request.Symbol, request.Category, cancellationToken: cancellationToken))
        {
            throw UnitDuplicateException.ForSymbol(request.Symbol, request.Category);
        }

        var unit = new UnitDefinition(
            request.Symbol,
            request.DisplayName,
            request.Category,
            request.MultiplierToBase,
            request.OffsetToBase);

        var initialStatus = _currentUser.IsAdmin
            ? UnitApprovalStatus.Approved
            : UnitApprovalStatus.PendingApproval;

        var id = await _repository.AddAsync(unit, _currentUser.Email, initialStatus, cancellationToken);
        var created = await _repository.GetEntryByIdAsync(id, cancellationToken)
            ?? throw new UnitCatalogException(UnitCatalogMessages.UnitCreatedButNotLoaded);

        return ToResponse(created);
    }

    /// <inheritdoc />
    public async Task<UnitDetailResponse> UpdateAsync(
        int id,
        UpdateUnitRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (await _repository.GetEntryByIdAsync(id, cancellationToken) is null)
        {
            throw UnitNotFoundException.ForId(id);
        }

        await ThrowIfInvalidAsync(_updateValidator, request, cancellationToken);

        var requiresReapproval = !_currentUser.IsAdmin;

        await _repository.UpdateAsync(
            id,
            request.DisplayName.Trim(),
            request.MultiplierToBase,
            request.OffsetToBase,
            _currentUser.Email,
            requiresReapproval,
            cancellationToken);

        var updated = await _repository.GetEntryByIdAsync(id, cancellationToken)
            ?? throw UnitNotFoundException.ForId(id);

        return ToResponse(updated);
    }

    /// <inheritdoc />
    public async Task<UnitDetailResponse> ApproveAsync(int id, CancellationToken cancellationToken = default)
    {
        await _repository.ApproveAsync(id, _currentUser.Email, cancellationToken);
        var approved = await _repository.GetEntryByIdAsync(id, cancellationToken)
            ?? throw UnitNotFoundException.ForId(id);

        return ToResponse(approved);
    }

    public async Task<UnitDetailResponse> RejectAsync(
        int id,
        RejectUnitRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        await ThrowIfInvalidAsync(_rejectValidator, request, cancellationToken);
        await _repository.RejectAsync(id, _currentUser.Email, request.Reason, cancellationToken);
        var rejected = await _repository.GetEntryByIdAsync(id, cancellationToken)
            ?? throw UnitNotFoundException.ForId(id);

        return ToResponse(rejected);
    }

    /// <inheritdoc />
    public Task DeleteAsync(int id, CancellationToken cancellationToken = default) =>
        _repository.DeleteAsync(id, cancellationToken);

    public async Task<UnitDetailResponse?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var entry = await _repository.GetEntryByIdAsync(id, cancellationToken);
        return entry is null ? null : ToResponse(entry);
    }

    /// <inheritdoc />
    public async Task<PagedResult<UnitDetailResponse>> ListAsync(
        UnitCategory? category,
        PagedRequest paging,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(paging);

        var (page, size) = PagingLimits.Normalize(paging);

        var paged = await _repository.ListEntriesPagedAsync(category, page, size, cancellationToken);
        var items = paged.Items.Select(ToResponse).ToList();

        return new PagedResult<UnitDetailResponse>(items, paged.TotalCount, page, size);
    }

    private static async Task ThrowIfInvalidAsync<T>(
        IValidator<T> validator,
        T instance,
        CancellationToken cancellationToken)
    {
        var result = await validator.ValidateAsync(instance, cancellationToken);
        if (!result.IsValid)
        {
            throw new ValidationException(result.Errors);
        }
    }

    private static UnitDetailResponse ToResponse(CatalogUnit unit) =>
        new(
            unit.Id,
            unit.Symbol,
            unit.Name,
            unit.Category,
            unit.MultiplierToBase,
            unit.OffsetToBase,
            unit.IsBaseUnit,
            unit.ApprovalStatus,
            unit.SubmittedBy,
            unit.SubmittedAt,
            unit.ApprovedBy,
            unit.ApprovedAt,
            unit.RejectionReason,
            unit.CreatedDate,
            unit.CreatedBy,
            unit.ModifiedDate,
            unit.ModifiedBy);
}
