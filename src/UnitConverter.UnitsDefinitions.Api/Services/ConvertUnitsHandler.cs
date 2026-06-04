using UnitConverter.UnitsDefinitions.Contracts.Requests;
using UnitConverter.UnitsDefinitions.Contracts.Responses;
using UnitConverter.UnitsDefinitions.Catalog;
using UnitConverter.UnitsDefinitions.Conversion;
using UnitConverter.UnitsDefinitions.Contracts.Units;

namespace UnitConverter.UnitsDefinitions.Api.Services;

/// <inheritdoc />
public sealed class ConvertUnitsHandler : IConvertUnitsHandler
{
    private readonly IUnitCatalogRepository _catalog;
    private readonly IConversionEngine _engine;

    public ConvertUnitsHandler(IUnitCatalogRepository catalog, IConversionEngine engine)
    {
        _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        _engine = engine ?? throw new ArgumentNullException(nameof(engine));
    }

    /// <inheritdoc />
    public async Task<ConversionResponse> HandleAsync(
        ConvertUnitsRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var category = request.Category
            ?? await ResolveCategoryAsync(request.FromUnit, cancellationToken);

        var units = await _catalog.GetByCategoryAsync(category, cancellationToken);
        var lookup = units.ToDictionary(u => u.Symbol, StringComparer.OrdinalIgnoreCase);

        var result = _engine.Convert(request.Value, request.FromUnit, request.ToUnit, category, lookup);

        return new ConversionResponse(
            result,
            request.ToUnit.Trim().ToLowerInvariant());
    }

    private async Task<UnitCategory> ResolveCategoryAsync(string fromUnit, CancellationToken cancellationToken)
    {
        foreach (UnitCategory category in Enum.GetValues<UnitCategory>())
        {
            var unit = await _catalog.FindBySymbolAsync(fromUnit, category, cancellationToken);
            if (unit is not null)
            {
                return category;
            }
        }

        throw new ConversionException(
            string.Format(ConversionMessages.CategoryNotResolvedFormat, fromUnit));
    }
}
