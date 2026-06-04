namespace UnitConverter.Common.Contracts.Paging;

/// <summary>
/// Paged list request parameters (shared across APIs and application layers).
/// </summary>
public sealed record PagedRequest(
    int Page = 1,
    int PageSize = 20);
