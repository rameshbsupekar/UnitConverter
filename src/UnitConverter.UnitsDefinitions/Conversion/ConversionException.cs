namespace UnitConverter.UnitsDefinitions.Conversion;

/// <summary>
/// Thrown when conversion cannot be performed (unknown units, invalid category).
/// </summary>
public sealed class ConversionException : Exception
{
    public ConversionException(string message)
        : base(message)
    {
    }
}
