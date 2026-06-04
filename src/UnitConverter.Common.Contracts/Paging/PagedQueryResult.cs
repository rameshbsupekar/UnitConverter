namespace UnitConverter.Common.Contracts.Paging;

/// <summary>
/// Result of a single page query from persistence (items + total row count).
/// </summary>
public sealed record PagedQueryResult<T>(
    IReadOnlyList<T> Items,
    int TotalCount);
