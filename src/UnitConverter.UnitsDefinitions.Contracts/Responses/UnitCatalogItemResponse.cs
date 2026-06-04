namespace UnitConverter.UnitsDefinitions.Contracts.Responses;

/// <summary>
/// An approved unit available for conversion in a category.
/// </summary>
public sealed record UnitCatalogItemResponse(
    string Symbol,
    string DisplayName);
