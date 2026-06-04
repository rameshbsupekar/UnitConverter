using UnitConverter.UnitsDefinitions.Contracts.Requests;
using UnitConverter.UnitsDefinitions.Contracts.Responses;

namespace UnitConverter.UnitsDefinitions.Api.Services;

/// <summary>
/// Handles unit conversion requests against the approved catalog.
/// </summary>
public interface IConvertUnitsHandler
{
    /// <summary>Converts a value from one unit to another within a category.</summary>
    Task<ConversionResponse> HandleAsync(
        ConvertUnitsRequest request,
        CancellationToken cancellationToken = default);
}
