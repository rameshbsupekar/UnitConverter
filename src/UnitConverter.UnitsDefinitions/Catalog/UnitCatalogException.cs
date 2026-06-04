namespace UnitConverter.UnitsDefinitions.Catalog;

/// <summary>
/// Thrown when catalog operations fail validation or business rules (maps to HTTP 400).
/// Use <see cref="UnitNotFoundException"/> or <see cref="UnitDuplicateException"/> when applicable.
/// </summary>
public class UnitCatalogException : Exception
{
    public UnitCatalogException(string message) : base(message)
    {
    }

    public UnitCatalogException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
