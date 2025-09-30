// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

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
        var results = dataSet.WhereDateIsNotEmpty(p => p.SessionBegin);

        // then
        results.ShouldBeEmpty();
    }
}
