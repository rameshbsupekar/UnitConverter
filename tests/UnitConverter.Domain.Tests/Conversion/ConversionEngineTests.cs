using UnitConverter.UnitsDefinitions.Contracts.Units;
using UnitConverter.UnitsDefinitions.Conversion;
using UnitConverter.UnitsDefinitions.Units;

namespace UnitConverter.UnitsDefinitions.Tests.Conversion;

[TestClass]
public sealed class ConversionEngineTests
{
    private readonly ConversionEngine _engine = new();

    [TestMethod]
    public void Convert_Length_MetersToFeet_ReturnsExpected()
    {
        var units = LengthUnits();
        var result = _engine.Convert(1m, "m", "ft", UnitCategory.Length, units);
        Assert.AreEqual(3.28084m, result, 0.0001m);
    }

    [TestMethod]
    public void Convert_Length_CentimetersToMeters_ReturnsExpected()
    {
        var units = FullLengthUnits();
        var result = _engine.Convert(100m, "cm", "m", UnitCategory.Length, units);
        Assert.AreEqual(1m, result);
    }

    [TestMethod]
    public void Convert_Temperature_CelsiusToFahrenheit_ReturnsExpected()
    {
        var units = TemperatureUnits();
        var result = _engine.Convert(0m, "c", "f", UnitCategory.Temperature, units);
        Assert.AreEqual(32m, result, 0.0001m);
    }

    [TestMethod]
    public void UnitDefinition_NonPositiveMultiplier_Throws()
    {
        Assert.ThrowsExactly<ArgumentException>(() =>
            new UnitDefinition("bad", "Bad", UnitCategory.Length, 0m));
    }

    [TestMethod]
    public void Convert_Temperature_FahrenheitToKelvin_MatchesAffineFormula()
    {
        var units = TemperatureUnits();
        var result = _engine.Convert(32m, "f", "k", UnitCategory.Temperature, units);
        Assert.AreEqual(273.15m, result, 0.0001m);
    }

    private static Dictionary<string, UnitDefinition> LengthUnits() =>
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["m"] = new("m", "Meter", UnitCategory.Length, 1m, 0m),
            ["ft"] = new("ft", "Foot", UnitCategory.Length, 0.3048m, 0m)
        };

    private static Dictionary<string, UnitDefinition> FullLengthUnits() =>
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["m"] = new("m", "Meter", UnitCategory.Length, 1m, 0m),
            ["cm"] = new("cm", "Centimeter", UnitCategory.Length, 0.01m, 0m)
        };

    private static Dictionary<string, UnitDefinition> TemperatureUnits() =>
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["k"] = new("k", "Kelvin", UnitCategory.Temperature, 1m, 0m),
            ["c"] = new("c", "Celsius", UnitCategory.Temperature, 1m, 273.15m),
            ["f"] = new("f", "Fahrenheit", UnitCategory.Temperature, 0.555555555555555556m, 255.372222222222222m)
        };
}
