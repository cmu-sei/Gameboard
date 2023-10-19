using Gameboard.Api.Data;
using Gameboard.Api.Features.GameEngine;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Tests.Integration;

[Collection(TestCollectionNames.DbFixtureTests)]
public class ChallengeControllerGradeAutoBonusTests
{
    private readonly GameboardTestContext _testContext;

    public ChallengeControllerGradeAutoBonusTests(GameboardTestContext testContext)
    {
        _testContext = testContext;
    }

    [Theory, GbIntegrationAutoData]
    public async Task Grade_WithSingleUnawardedSolveRankBonus_AwardsBonus
    (
        string bonusId,
        string challengeId,
        string challengeSpecId,
        string gameId,
        string teamId,
        IFixture fixture
    )
    {
        var baseScore = 100;
        var bonus = 20;

        // given
        await _testContext
            .WithDataState(state =>
            {
                state.Add<Data.Game>(fixture, g => g.Id = gameId);
                state.Add<Data.ChallengeSpec>(fixture, spec =>
                {
                    spec.Id = challengeSpecId;
                    spec.GameId = gameId;
                    spec.Points = (int)baseScore;
                    spec.Bonuses = new List<Data.ChallengeBonus>()
                    {
                        new ChallengeBonusCompleteSolveRank
                        {
                            Id = bonusId,
                            PointValue = bonus,
                            SolveRank = 1
                        }
                    };
                });

                state.AddChallenge(c =>
                {
                    c.Id = challengeId;
                    c.GameId = gameId;
                    c.Player = state.Build<Data.Player>(fixture, p =>
                    {
                        p.TeamId = teamId;
                    });
                    c.SpecId = challengeSpecId;
                    c.TeamId = teamId;
                });
            });

        var submission = fixture.Create<GameEngineSectionSubmission>();
        submission.Id = challengeId;

        var http = _testContext.CreateHttpClientWithAuthRole(UserRole.Admin);

        // when
        await http
            .PutAsync("/api/challenge/grade", submission.ToJsonBody())
            .WithContentDeserializedAs<Api.Challenge>();

        // tricky to validate this - the endpoint is pinned to returning a challenge state, which doesn't include bonuses yet.
        // have to go to the DB to minimize false positives
        var awardedBonus = await _testContext
            .GetDbContext()
            .AwardedChallengeBonuses
            .Include(b => b.ChallengeBonus)
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.ChallengeId == challengeId);

        // then
        awardedBonus.ShouldNotBeNull();
        awardedBonus.ChallengeBonus.PointValue.ShouldBe(bonus);
    }

    [Theory, GbIntegrationAutoData]
    public async Task Grade_WithSingleUnawardedSolveRankBonusAndPartialSolve_DoesNotAwardBonus
    (
        string bonusId,
        string challengeId,
        string challengeSpecId,
        string gameId,
        string teamId,
        IFixture fixture
    )
    {
        var fullSolveScore = 150;
        var bonus = 20;

        // given
        await _testContext
            .WithDataState(state =>
            {
                state.Add<Data.Game>(fixture, g => g.Id = gameId);
                state.Add<Data.ChallengeSpec>(fixture, spec =>
                {
                    spec.Id = challengeSpecId;
                    spec.GameId = gameId;
                    spec.Points = fullSolveScore;
                    spec.Bonuses = new List<Data.ChallengeBonus>
                    {
                        new ChallengeBonusCompleteSolveRank
                        {
                            Id = bonusId,
                            PointValue = bonus,
                            SolveRank = 1
                        }
                    };
                });

                state.Add<Data.Challenge>(fixture, c =>
                {
                    c.Id = challengeId;
                    c.GameId = gameId;
                    c.Player = state.Build<Data.Player>(fixture, p =>
                    {
                        p.TeamId = teamId;
                    });
                    c.Points = fullSolveScore;
                    c.SpecId = challengeSpecId;
                    c.TeamId = teamId;
                });
            });

        var submission = fixture.Create<GameEngineSectionSubmission>();
        submission.Id = challengeId;

        var http = _testContext.CreateHttpClientWithAuthRole(UserRole.Admin);

        // when
        await http
            .PutAsync("/api/challenge/grade", submission.ToJsonBody())
            .WithContentDeserializedAs<Challenge>();

        // tricky to validate this - the endpoint is pinned to returning a challenge state, which doesn't include bonuses yet.
        // have to go to the DB to minimize false positives
        var awardedBonus = await _testContext
            .GetDbContext()
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
        string awardedChallengeId,
        string unawardedChallengeId,
        string challengeSpecId,
        string awardedBonusId,
        string unawardedBonusId,
        string awardedTeamId,
        string unawardedTeamId,
        string gameId,
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
                state.Add<Data.Game>(fixture, g => g.Id = gameId);
                state.Add<Data.ChallengeSpec>(fixture, spec =>
                {
                    spec.Id = challengeSpecId;
                    spec.GameId = gameId;
                    spec.Points = baseScore;
                    spec.Bonuses = new List<Data.ChallengeBonus>
                    {
                        new ChallengeBonusCompleteSolveRank
                        {
                            Id = awardedBonusId,
                            PointValue = awardedBonusPoints,
                            SolveRank = 1
                        },
                        new ChallengeBonusCompleteSolveRank
                        {
                            Id = unawardedBonusId,
                            PointValue = unawardedBonusPoints,
                            SolveRank = 2
                        }
                    };
                });

                // 2 teams, one with the first bonus already awarded
                state.AddChallenge(c =>
                {
                    c.Id = awardedChallengeId;
                    c.GameId = gameId;
                    c.Player = state.Build<Data.Player>(fixture, p =>
                    {
                        p.GameId = gameId;
                        p.TeamId = awardedTeamId;
                    });
                    c.SpecId = challengeSpecId;
                    c.StartTime = DateTimeOffset.UtcNow;
                    c.EndTime = c.StartTime.AddSeconds(30);
                    c.TeamId = awardedTeamId;
                    c.AwardedBonuses = new List<AwardedChallengeBonus> { new AwardedChallengeBonus { Id = awardedBonusId } };
                });

                state.AddChallenge(c =>
                {
                    c.Id = unawardedChallengeId;
                    c.GameId = gameId;
                    c.Player = state.Build<Data.Player>(fixture, p =>
                    {
                        p.GameId = gameId;
                        p.TeamId = unawardedTeamId;
                    });
                    c.SpecId = challengeSpecId;
                    c.TeamId = unawardedTeamId;
                });
            });

        var submission = fixture.Create<GameEngineSectionSubmission>();
        submission.Id = unawardedChallengeId;

        var http = _testContext.CreateHttpClientWithAuthRole(UserRole.Admin);

        // when
        await http
            .PutAsync("/api/challenge/grade", submission.ToJsonBody())
            .WithContentDeserializedAs<Api.Challenge>();

        // tricky to validate this - the endpoint is pinned to returning a challenge state, which doesn't include bonuses yet.
        // have to go to the DB to minimize false positives
        var awardedBonus = await _testContext
            .GetDbContext()
            .AwardedChallengeBonuses
            .Include(b => b.ChallengeBonus)
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.ChallengeId == unawardedChallengeId);

        // then
        awardedBonus.ShouldNotBeNull();
        awardedBonus.ChallengeBonus.PointValue.ShouldBe(unawardedBonusPoints);
    }
}