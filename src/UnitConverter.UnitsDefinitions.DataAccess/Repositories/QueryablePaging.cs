using Microsoft.EntityFrameworkCore;

namespace UnitConverter.UnitsDefinitions.DataAccess.Repositories;

/// <summary>
/// EF Core offset pagination (<see href="https://learn.microsoft.com/en-us/ef/core/querying/pagination">docs</see>).
/// </summary>
internal static class QueryablePaging
{
    public static async Task<(IReadOnlyList<T> Items, int TotalCount)> ToPageAsync<T>(
        IQueryable<T> orderedQuery,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var totalCount = await orderedQuery.CountAsync(cancellationToken);
        if (totalCount == 0)
        {
            return (Array.Empty<T>(), 0);
        }

        var items = await orderedQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }
}
