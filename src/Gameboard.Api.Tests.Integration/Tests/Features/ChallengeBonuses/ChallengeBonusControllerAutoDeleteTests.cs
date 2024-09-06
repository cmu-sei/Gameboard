using Gameboard.Api.Common;
using Gameboard.Api.Data;
using Gameboard.Api.Features.ChallengeBonuses;
using Gameboard.Api.Structure;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Tests.Integration.ChallengeBonuses;

public class ChallengeBonusControllerAutoDeleteTests : IClassFixture<GameboardTestContext>
{
    private readonly GameboardTestContext _testContext;

    public ChallengeBonusControllerAutoDeleteTests(GameboardTestContext testContext)
    {
        _testContext = testContext;
    }

    [Theory, GbIntegrationAutoData]
    public async Task DeleteGameAutoBonuses_WithConfiguredBonuses_Deletes(string gameId, IFixture fixture)
    {
        // given: a configured challenge
        await _testContext
            .WithDataState(state =>
            {
                state.Add<Data.Game>(fixture, g => g.Id = gameId);
                state.Add<Data.ChallengeSpec>(fixture, spec =>
                {
                    spec.GameId = gameId;
                    spec.Bonuses = new ChallengeBonus[]
                    {
                        new ChallengeBonusCompleteSolveRank
                        {
                            Id = fixture.Create<string>(),
                            Description = fixture.Create<string>(),
                            PointValue = fixture.Create<int>(),
                            ChallengeBonusType = ChallengeBonusType.CompleteSolveRank
                        }
                    };
                });
            });

        var httpClient = _testContext.CreateHttpClientWithAuthRole(UserRole.Tester);

        // when delete is called
        await httpClient.DeleteAsync($"api/game/{gameId}/bonus/config");

        // then there should be no challenges assigned to a spec with the given gameId
        var count = await _testContext
            .GetDbContext()
            .ChallengeBonuses
            .AsNoTracking()
            .Include(b => b.ChallengeSpec)
            .Where(b => b.ChallengeSpec.GameId == gameId)
            .CountAsync();

        count.ShouldBe(0);
    }

    [Theory, GbIntegrationAutoData]
    public async Task DeleteGameAutoBonuses_WithBonusesAwarded_FailsValidation(string gameId, string challengeId, IFixture fixture)
    {
        // given: a game with awarded challenge bonuses
        await _testContext
            .WithDataState(state =>
            {
                state.Add<Data.Game>(fixture, g =>
                {
                    g.Id = gameId;
                    g.Challenges = state.Build<Data.Challenge>(fixture, c => c.Id = challengeId).ToCollection();
                    g.Specs = state.Build<Data.ChallengeSpec>(fixture, s =>
                    {
                        s.Bonuses = new ChallengeBonusCompleteSolveRank
                        {
                            Id = fixture.Create<string>(),
                            Description = fixture.Create<string>(),
                            PointValue = fixture.Create<int>(),
                            ChallengeBonusType = ChallengeBonusType.CompleteSolveRank,
                            AwardedTo = new AwardedChallengeBonus { Id = fixture.Create<string>(), ChallengeId = challengeId }.ToCollection()
                        }.ToCollection<Data.ChallengeBonus>();
                    }).ToCollection();
                });
            });

        var httpClient = _testContext.CreateHttpClientWithAuthRole(UserRole.Admin);

        // when delete is called, then it should fail validation
        await httpClient
            .DeleteAsync($"api/game/{gameId}/bonus/config")
            .ShouldYieldGameboardValidationException<GameboardAggregatedValidationExceptions>();
    }
}
