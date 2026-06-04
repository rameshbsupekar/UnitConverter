using UnitConverter.UnitsDefinitions.Contracts.Units;

namespace UnitConverter.UnitsDefinitions.Catalog;

/// <summary>
/// Thrown when a unit symbol already exists in a category.
/// </summary>
public sealed class UnitDuplicateException : UnitCatalogException
{
    public UnitDuplicateException(string message)
        : base(message)
    {
    }

    public static UnitDuplicateException ForSymbol(string symbol, UnitCategory category) =>
        new(string.Format(UnitCatalogMessages.DuplicateUnitFormat, symbol, category));
}
