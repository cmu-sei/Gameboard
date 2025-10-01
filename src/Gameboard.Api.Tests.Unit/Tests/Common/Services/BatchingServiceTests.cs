// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using Gameboard.Api.Common.Services;
using Microsoft.Extensions.Logging;

namespace Gameboard.Api.Tests.Unit;

public class BatchingServiceTests
{
    [Theory, GameboardAutoData]
    public void BuildBatches_WithFixedSizeAndItemCount_ReturnsExpectedBatchCount(IFixture fixture)
    {
        // given a deploy request with 17 challenges and a batch size of 6
        var items = fixture.CreateMany<int>(17);
        var batchSize = 6;

        // create sut and its options
        var sut = new BatchingService(A.Fake<ILogger<BatchingService>>());

        // when batches are built
        var result = sut.Batch(items, batchSize);

        // we expect three batches
        result.Count().ShouldBe(3);
        // and the last should have 5 tasks in it
        result.Last().Count().ShouldBe(5);
    }
}
