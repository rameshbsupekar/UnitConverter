namespace UnitConverter.UnitsDefinitions.Contracts.Responses;

/// <summary>
/// A conversion dimension exposed on the public catalog API.
/// </summary>
public sealed record UnitCategoryResponse(
    int Id,
    string Name,
    string DisplayName);
