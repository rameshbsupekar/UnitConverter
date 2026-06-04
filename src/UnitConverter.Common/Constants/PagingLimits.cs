using UnitConverter.Common.Contracts.Paging;

namespace UnitConverter.Common.Constants;

/// <summary>
/// Bounds for API paging query parameters.
/// </summary>
public static class PagingLimits
{
    public const int MinPage = 1;
    public const int MinPageSize = 1;
    public const int MaxPageSize = 100;

    public static (int Page, int PageSize) Normalize(PagedRequest paging)
    {
        ArgumentNullException.ThrowIfNull(paging);
        var page = Math.Max(MinPage, paging.Page);
        var pageSize = Math.Clamp(paging.PageSize, MinPageSize, MaxPageSize);
        return (page, pageSize);
    }
}
