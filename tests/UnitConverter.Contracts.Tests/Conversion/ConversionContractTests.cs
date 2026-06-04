using System.Text.Json;
using UnitConverter.UnitsDefinitions.Contracts.Requests;
using UnitConverter.UnitsDefinitions.Contracts.Responses;
using UnitConverter.UnitsDefinitions.Contracts.Units;
using Xunit;

namespace UnitConverter.Contracts.Tests.Conversion;

public sealed class ConversionContractTests
{
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    [Fact]
    public void ConvertUnitsRequest_SerializesAndDeserializesCorrectly()
    {
        var request = new ConvertUnitsRequest(
            Value: 100m,
            FromUnit: "m",
            ToUnit: "ft",
            Category: UnitCategory.Length);

        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var deserialized = JsonSerializer.Deserialize<ConvertUnitsRequest>(json, _jsonOptions);

        Assert.NotNull(deserialized);
        Assert.Equal(request.Value, deserialized.Value);
        Assert.Equal(request.FromUnit, deserialized.FromUnit);
        Assert.Equal(request.ToUnit, deserialized.ToUnit);
        Assert.Equal(request.Category, deserialized.Category);
    }

    [Fact]
    public void ConversionResponse_RecordEquality()
    {
        var a = new ConversionResponse(3.28084m, "ft");
        var b = new ConversionResponse(3.28084m, "ft");
        Assert.Equal(a, b);
    }
}
