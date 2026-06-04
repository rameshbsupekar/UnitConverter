namespace UnitConverter.Common.Constants;

/// <summary>
/// RFC 7807 problem detail titles.
/// </summary>
public static class ProblemTitles
{
    public const string BadRequest = "Bad Request";
    public const string Unauthorized = "Unauthorized";
    public const string Forbidden = "Forbidden";
    public const string NotFound = "Not Found";
    public const string Conflict = "Conflict";
    public const string TooManyRequests = "Too Many Requests";
    public const string InternalServerError = "Internal Server Error";

    public static readonly string AccessDeniedDetail = "Access denied";
    public static readonly string RateLimitDetail = "Rate limit exceeded";
    public static readonly string UnexpectedErrorDetail = "An unexpected error occurred";
}
