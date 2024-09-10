using Gameboard.Api.Common;
using Gameboard.Api.Data;
using Gameboard.Api.Features.GameEngine;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Tests.Integration;

public class ChallengeControllerGradeAutoBonusTests(GameboardTestContext testContext) : IClassFixture<GameboardTestContext>
{
    private readonly GameboardTestContext _testContext = testContext;

    [Theory, GbIntegrationAutoData]
    public async Task Grade_WithSingleUnawardedSolveRankBonus_AwardsBonus
    (
        string bonusId,
        string challengeId,
        string challengeSpecId,
        string gameId,
        string graderKey,
        string teamId,
        IFixture fixture
    )
    {
        var baseScore = 100;
        var bonusPoints = 20;

        // given
        await _testContext
            .WithDataState(state =>
            {
                var bonus = state.Build<ChallengeBonusCompleteSolveRank>(fixture, b =>
                {
                    b.Id = bonusId;
                    b.ChallengeBonusType = ChallengeBonusType.CompleteSolveRank;
                    b.PointValue = bonusPoints;
                    b.SolveRank = 1;
                });

                state.Add<Data.Game>(fixture, g =>
                {
                    g.Id = gameId;
                    g.Specs = state.Build<Data.ChallengeSpec>(fixture, spec =>
                    {
                        spec.Id = challengeSpecId;
                        spec.Points = baseScore;
                        spec.Bonuses = (bonus as ChallengeBonus).ToCollection();
                    }).ToCollection();

                    g.Challenges = state.Build<Data.Challenge>(fixture, c =>
                    {
                        c.Id = challengeId;
                        c.GraderKey = graderKey.ToSha256();
                        c.Player = state.Build<Data.Player>(fixture, p =>
                        {
                            p.GameId = gameId;
                            p.TeamId = teamId;
                        });
                        c.SpecId = challengeSpecId;
                        c.TeamId = teamId;
                    }).ToCollection();
                });
            });


        var submission = fixture.Create<GameEngineSectionSubmission>();
        submission.Id = challengeId;

        // when
        await _testContext
            .CreateHttpClientWithGraderConfig(100, graderKey)
            .PutAsync("/api/challenge/grade", submission.ToJsonBody())
            .DeserializeResponseAs<Api.Challenge>();

        // tricky to validate this - the endpoint is pinned to returning a challenge state, which doesn't include bonuses yet.
        // have to go to the DB to minimize false positives
        await _testContext.ValidateStoreStateAsync(async db =>
        {
            var awardedBonus = await db
                .AwardedChallengeBonuses
                .Include(b => b.ChallengeBonus)
                .Include(b => b.Challenge)
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.ChallengeId == challengeId);

            // then
            awardedBonus.ShouldNotBeNull();
            awardedBonus.ChallengeBonus.PointValue.ShouldBe(bonusPoints);
            awardedBonus.Challenge.Score.ShouldBe(baseScore);
        });
    }

    [Theory, GbIntegrationAutoData]
    public async Task Grade_WithSingleUnawardedSolveRankBonusAndPartialSolve_DoesNotAwardBonus
(
    string bonusId,
    string challengeId,
    string challengeSpecId,
    string gameId,
    string graderKey,
    string teamId,
    IFixture fixture
)
    {
        var partialSolveScore = 120;
        var fullSolveScore = 150;
        var bonus = 20;

        // given
        await _testContext
            .WithDataState(state =>
            {
                state.Add<Data.Game>(fixture, g =>
                {
                    g.Id = gameId;

                    g.Specs = state.Build<Data.ChallengeSpec>(fixture, spec =>
                    {
                        spec.Id = challengeSpecId;
                        spec.GameId = gameId;
                        spec.Points = fullSolveScore;
                        spec.Bonuses =
                        [
                            new ChallengeBonusCompleteSolveRank
                            {
                                Id = bonusId,
                                PointValue = bonus,
                                SolveRank = 1
                            }
                        ];
                    }).ToCollection();

                    g.Challenges = state.Build<Data.Challenge>(fixture, c =>
                    {
                        c.Id = challengeId;
                        c.GraderKey = graderKey.ToSha256();
                        c.Player = state.Build<Data.Player>(fixture, p =>
                        {
                            p.GameId = gameId;
                            p.TeamId = teamId;
                        });
                        c.Points = fullSolveScore;
                        c.SpecId = challengeSpecId;
                        c.TeamId = teamId;
                    }).ToCollection();
                });
            });

        var submission = fixture.Create<GameEngineSectionSubmission>();
        submission.Id = challengeId;

        // when
        await _testContext
            .CreateHttpClientWithGraderConfig(partialSolveScore, graderKey)
            .PutAsync("/api/challenge/grade", submission.ToJsonBody())
            .DeserializeResponseAs<Challenge>();

        // tricky to validate this - the endpoint is pinned to returning a challenge state, which doesn't include bonuses yet.
        // have to go to the DB to minimize false positives
        var awardedBonus = await _testContext
            .GetValidationDbContext()
            .AwardedChallengeBonuses
            .Include(b => b.ChallengeBonus)
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.ChallengeId == challengeId);

        // then
        awardedBonus.ShouldBeNull();
    }

    [Theory, GbIntegrationAutoData]
    public async Task Grade_WithAwardedAndUnawardedSolveRankBonusAndSolve_Awards2ndBonus
    (
        string awardedBonusId,
        string awardedTeamId,
        string unawardedChallengeId,
        string challengeSpecId,
        string unawardedTeamId,
        string gameId,
        string graderKey,
        IFixture fixture
    )
    {
        var baseScore = 100;
        var awardedBonusPoints = 50;
        var unawardedBonusPoints = 20;

        // given
        await _testContext
            .WithDataState(state =>
            {
                state.Add<Data.Game>(fixture, g =>
                {
                    g.Id = gameId;
                    g.GameStart = DateTimeOffset.UtcNow.AddDays(-1);

                    g.Specs = state.Build<Data.ChallengeSpec>(fixture, spec =>
                    {
                        spec.Id = challengeSpecId;
                        spec.GameId = gameId;
                        spec.Points = baseScore;
                        spec.Bonuses =
                        [
                            state.Build<ChallengeBonusCompleteSolveRank>(fixture, cb =>
                            {
                                cb.Id = awardedBonusId;
                                cb.PointValue = awardedBonusPoints;
                                cb.SolveRank = 1;
                            }),
                            state.Build<ChallengeBonusCompleteSolveRank>(fixture, cb =>
                            {
                                cb.PointValue = unawardedBonusPoints;
                                cb.SolveRank = 2;
                            })
                        ];
                    }).ToCollection();

                    // 2 teams, one with the first bonus already awarded
                    g.Players =
                    [
                        state.Build<Data.Player>(fixture, p =>
                        {
                            p.TeamId = awardedTeamId;
                            p.Challenges = state.Build<Data.Challenge>(fixture, c =>
                            {
                                c.EndTime = DateTimeOffset.UtcNow;
                                c.GameId = gameId;
                                c.Game = null;
                                c.Points = baseScore;
                                c.Score = baseScore;
                                c.SpecId = challengeSpecId;
                                c.AwardedBonuses = state.Build<AwardedChallengeBonus>(fixture, b => b.ChallengeBonusId = awardedBonusId).ToCollection();
                                c.TeamId = awardedTeamId;
                            }).ToCollection();
                        }),

                        state.Build<Data.Player>(fixture, p =>
                        {
                            p.TeamId = unawardedTeamId;
                            p.Challenges = state.Build<Data.Challenge>(fixture, c =>
                            {
                                c.Id = unawardedChallengeId;
                                c.EndTime = DateTimeOffset.UtcNow;
                                c.GameId = gameId;
                                c.Game = null;
                                c.Points = baseScore;
                                c.Score = baseScore;
                                c.GraderKey = graderKey.ToSha256();
                                c.SpecId = challengeSpecId;
                                c.TeamId = unawardedTeamId;
                            }).ToCollection();
                        })
                    ];
                });
            });

        var submission = fixture.Create<GameEngineSectionSubmission>();
        submission.Id = unawardedChallengeId;

        // when
        await _testContext
            .CreateHttpClientWithGraderConfig(baseScore, graderKey)
            .PutAsync("/api/challenge/grade", submission.ToJsonBody())
            .DeserializeResponseAs<Challenge>();

        // tricky to validate this - the endpoint is pinned to returning a challenge state, which doesn't include bonuses yet.
        // have to go to the DB to minimize false positives
        var dbContext = _testContext.GetValidationDbContext();
        var awardedBonus = await dbContext
            .AwardedChallengeBonuses
            .Include(b => b.ChallengeBonus)
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.ChallengeId == unawardedChallengeId);

        // then
        awardedBonus.ShouldNotBeNull();
        awardedBonus.ChallengeBonus.PointValue.ShouldBe(unawardedBonusPoints);
    }
}
