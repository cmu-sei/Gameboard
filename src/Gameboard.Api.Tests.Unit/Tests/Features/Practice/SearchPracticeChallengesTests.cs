using Gameboard.Api.Common;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Challenges;
using Gameboard.Api.Features.Practice;
using Gameboard.Api.Features.Users;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Tests.Unit;

public class SearchPracticeChallengesTests
{
    [Theory, GameboardAutoData]
    public async Task SearchPracticeChallenges_WithDisabled_ReturnsEmpty(IFixture fixture)
    {
        // given a challenge which is disabled
        var disabledSpec = new Data.ChallengeSpec
        {
            Id = fixture.Create<string>(),
            Name = fixture.Create<string>(),
            Description = fixture.Create<string>(),
            Text = fixture.Create<string>(),
            Tags = fixture.Create<string>(),
            Disabled = true,
            Game = new Data.Game
            {
                Name = fixture.Create<string>(),
                PlayerMode = PlayerMode.Practice
            }
        };

        var sut = GetSutWithResults(fixture, disabledSpec);

        // when a query for all challenges is issued
        var query = await sut.BuildQuery(string.Empty, []);
        var result = await query.ToArrayAsync(CancellationToken.None);

        // then we expect no results
        result.Length.ShouldBe(0);
    }

    [Theory, GameboardAutoData]
    public async Task SearchPracticeChallenges_WithEnabled_Returns(IFixture fixture)
    {
        var enabledSpec = new Data.ChallengeSpec
        {
            Id = fixture.Create<string>(),
            Name = fixture.Create<string>(),
            Description = fixture.Create<string>(),
            Text = fixture.Create<string>(),
            Tags = fixture.Create<string>(),
            Disabled = false,
            Game = new Data.Game
            {
                Name = fixture.Create<string>(),
                PlayerMode = PlayerMode.Practice
            }
        };

        var sut = GetSutWithResults(fixture, enabledSpec);

        // when a query for all challenges is issued
        var query = await sut.BuildQuery(string.Empty, []);
        var result = await query.ToArrayAsync(CancellationToken.None);

        // then we expect one result
        result.Length.ShouldBe(1);
    }

    private SearchPracticeChallengesHandler GetSutWithResults(IFixture fixture, params Data.ChallengeSpec[] specs)
    {
        var queryResults = specs.BuildMock();

        var store = A.Fake<IStore>();
        A.CallTo(() => store.WithNoTracking<Data.ChallengeSpec>())
            .WithAnyArguments()
            .Returns(queryResults);

        var sut = new SearchPracticeChallengesHandler
        (
            A.Fake<IChallengeDocsService>(),
            A.Fake<IPagingService>(),
            A.Fake<IUserRolePermissionsService>(),
            A.Fake<IPracticeService>(),
            A.Fake<ISlugService>(),
            store
        );

        return sut;
    }
}
