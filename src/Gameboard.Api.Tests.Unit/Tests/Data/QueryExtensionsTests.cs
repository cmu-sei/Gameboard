using Gameboard.Api.Data;

namespace Gameboard.Api.Tests.Unit;

public class QueryExtensionsTests
{
    [Fact]
    public void WhereHasDateValue_WithMinDate_ExcludesFromResults()
    {
        // given
        var player = new Player { SessionBegin = DateTimeOffset.MinValue };
        var dataSet = new Player[] { player }.BuildMock();

        // when
        var results = dataSet.WhereDateHasValue(p => p.SessionBegin);

        // then
        results.ShouldBeEmpty();
    }
}
