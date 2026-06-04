namespace UnitConverter.UnitsDefinitions.Catalog;

/// <summary>
/// Thrown when a unit master-data row does not exist.
/// </summary>
public sealed class UnitNotFoundException : UnitCatalogException
{
    public UnitNotFoundException(string message)
        : base(message)
    {
    }

    public static UnitNotFoundException ForId(int id) =>
        new(string.Format(UnitCatalogMessages.UnitNotFoundByIdFormat, id));
}
