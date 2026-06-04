using UnitConverter.UnitsDefinitions.Contracts.Units;
using Xunit;

namespace UnitConverter.Contracts.Tests.Common;

public sealed class UnitCategoryTests
{
    [Theory]
    [InlineData(UnitCategoryWireNames.Length, UnitCategory.Length)]
    [InlineData(UnitCategoryWireNames.Temperature, UnitCategory.Temperature)]
    [InlineData(UnitCategoryWireNames.Mass, UnitCategory.Mass)]
    [InlineData(UnitCategoryWireNames.Weight, UnitCategory.Mass)]
    public void TryParseWireName_KnownValues_Succeeds(string wireName, UnitCategory expected)
    {
        Assert.True(UnitCategoryExtensions.TryParseWireName(wireName, out var category));
        Assert.Equal(expected, category);
    }

    [Fact]
    public void ToWireName_RoundTrips()
    {
        Assert.Equal(UnitCategoryWireNames.Length, UnitCategory.Length.ToWireName());
        Assert.Equal(UnitCategoryWireNames.Mass, UnitCategory.Mass.ToWireName());
    }
}
