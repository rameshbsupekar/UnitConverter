namespace UnitConverter.UnitsDefinitions.Conversion;

/// <summary>
/// User-facing conversion error messages.
/// </summary>
public static class ConversionMessages
{
    public const string UnknownSourceUnitFormat = "Unknown source unit '{0}'.";
    public const string UnknownTargetUnitFormat = "Unknown target unit '{0}'.";
    public const string InvalidMultipliers = "Invalid conversion multipliers.";
    public const string CategoryNotResolvedFormat =
        "Could not resolve category for unit '{0}'. Specify Category explicitly.";
}
