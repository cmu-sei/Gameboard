using Gameboard.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Tests.Integration.ChallengeBonuses;

public class ChallengeBonusControllerAutoDeleteTests : IClassFixture<GameboardTestContext<GameboardDbContextPostgreSQL>>
{
    private readonly GameboardTestContext<GameboardDbContextPostgreSQL> _testContext;

    public ChallengeBonusControllerAutoDeleteTests(GameboardTestContext<GameboardDbContextPostgreSQL> testContext)
    {
        _testContext = testContext;
    }

    [Theory, GbIntegrationAutoData]
    public async Task DeleteGameAutoBonuses_WithConfiguredBonuses_Deletes(string gameId, IFixture fixture)
    {
        // given: a configured challenge
        await _testContext
            .WithTestServices(s => s.AddGbIntegrationTestAuth(UserRole.Designer))
            .WithDataState(state =>
            {
                state.AddGame(gameId);
                state.AddChallengeSpec(spec =>
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

        // when delete is called
        await _testContext
            .Http
            .DeleteAsync($"api/game/{gameId}/bonus/config");

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
            .WithTestServices(s => s.AddGbIntegrationTestAuth(UserRole.Designer))
            .WithDataState(state =>
            {
                state.AddGame(gameId);
                state.AddChallenge(c =>
                {
                    c.Id = challengeId;
                    c.GameId = gameId;
                });

                state.AddChallengeSpec(spec =>
                {
                    spec.GameId = gameId;
                    spec.Bonuses = new ChallengeBonus[]
                    {
                        new ChallengeBonusCompleteSolveRank
                        {
                            Id = fixture.Create<string>(),
                            Description = fixture.Create<string>(),
                            PointValue = fixture.Create<int>(),
                            ChallengeBonusType = ChallengeBonusType.CompleteSolveRank,
                            AwardedTo = new AwardedChallengeBonus
                            {
                                Id = fixture.Create<string>(),
                                ChallengeId = challengeId
                            }.ToCollection()
                        }
                    };
                });
            });

        // when delete is called, then it should fail validation
        var isValidationException = await _testContext.Http
            .DeleteAsync($"api/game/{gameId}/bonus/config")
            .YieldsGameboardValidationException();

        isValidationException.ShouldBeTrue();
    }
}
