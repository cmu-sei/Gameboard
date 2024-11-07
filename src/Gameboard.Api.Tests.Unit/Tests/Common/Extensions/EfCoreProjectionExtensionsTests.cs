using Gameboard.Api.Common;

namespace Gameboard.Api.Tests.Unit;

public class EfCoreProjectionExtensionsTests
{
    [Theory, GameboardAutoData]
    public async Task ToLookupAsync_WithMultiples_ProducesSingleEntry(IFixture fixture)
    {
        // given one "group" with two members
        var id = fixture.Create<string>();
        var data = new List<SimpleEntity>()
        {
            new() { Id = id, Name = fixture.Create<string>() },
            new() { Id = id, Name = fixture.Create<string>() }
        }.BuildMock();

        // when we lookup them
        var result = await data.ToLookupAsync(e => e.Id, CancellationToken.None);

        // we should get a dictionary with a single entry and two things in the list
        result.Count.ShouldBe(1);
        result[id].Count.ShouldBe(2);
    }
}
