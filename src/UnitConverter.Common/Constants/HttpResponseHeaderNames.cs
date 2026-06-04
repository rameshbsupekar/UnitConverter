namespace UnitConverter.Common.Constants;

/// <summary>
/// Response header names not exposed as <see cref="Microsoft.AspNetCore.Http.IHeaderDictionary"/> properties.
/// For standard names, prefer <see cref="Microsoft.Net.Http.Headers.HeaderNames"/>.
/// </summary>
public static class HttpResponseHeaderNames
{
    /// <summary>
    /// Referrer-Policy is not in <see cref="Microsoft.Net.Http.Headers.HeaderNames"/> (unlike Referer).
    /// See: https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/Referrer-Policy
    /// </summary>
    public const string ReferrerPolicy = "Referrer-Policy";
}
