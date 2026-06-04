using UnitConverter.Common.Contracts.Paging;
using Xunit;

namespace UnitConverter.Contracts.Tests.Infrastructure;

public sealed class PagingContractTests
{
    [Fact]
    public void PagedResult_ComputesTotalPages()
    {
        var result = new PagedResult<string>(
            Items: new[] { "a", "b" },
            TotalCount: 25,
            Page: 2,
            PageSize: 10);

        Assert.Equal(3, result.TotalPages);
        Assert.True(result.HasPreviousPage);
        Assert.True(result.HasNextPage);
    }
}
