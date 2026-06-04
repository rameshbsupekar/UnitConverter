using UnitConverter.Common.Constants;
using UnitConverter.Common.Contracts.Paging;
using UnitConverter.UnitsDefinitions.Catalog;
using UnitConverter.UnitsDefinitions.Contracts.Responses;
using UnitConverter.UnitsDefinitions.Contracts.Units;

namespace UnitConverter.UnitsDefinitions.Api.Services;

/// <inheritdoc />
public sealed class CatalogQueries : ICatalogQueries
{
    private readonly IUnitCatalogRepository _catalog;

    public CatalogQueries(IUnitCatalogRepository catalog) =>
        _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));

    /// <inheritdoc />
    public Task<IReadOnlyList<UnitCategoryResponse>> GetCategoriesAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        IReadOnlyList<UnitCategoryResponse> categories = Enum.GetValues<UnitCategory>()
            .Select(category => new UnitCategoryResponse(
                (int)category,
                category.ToWireName(),
                category.ToDisplayName()))
            .OrderBy(c => c.Id)
            .ToList();

        return Task.FromResult(categories);
    }

    /// <inheritdoc />
    public async Task<PagedResult<UnitCatalogItemResponse>> GetUnitsByCategoryAsync(
        GetUnitsByCategoryQuery query,
        PagedRequest paging,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(paging);

        var (page, size) = PagingLimits.Normalize(paging);

        var paged = await _catalog.GetApprovedByCategoryPagedAsync(
            query.Category,
            page,
            size,
            cancellationToken);

        var pageItems = paged.Items
            .Select(u => new UnitCatalogItemResponse(u.Symbol, u.Name))
            .ToList();

        return new PagedResult<UnitCatalogItemResponse>(pageItems, paged.TotalCount, page, size);
    }
}
