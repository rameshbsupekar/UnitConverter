namespace UnitConverter.Common.Constants;

/// <summary>
/// RFC 7807 problem type URI templates.
/// </summary>
public static class ApiErrorTypes
{
    // RFC 7807 problem type URI prefix (documentation identifier, not a live endpoint).
#pragma warning disable S1075 // URIs are required by RFC 7807 problem type format
    public const string BaseUri = "https://api.unitconverter.dev/errors/";
#pragma warning restore S1075

    public static string ForStatusCode(int statusCode) => $"{BaseUri}{statusCode}";
}
