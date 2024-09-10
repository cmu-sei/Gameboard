using Gameboard.Api.Common;
using Gameboard.Api.Features.GameEngine;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Tests.Integration;

public class ChallengeControllerGradeTests(GameboardTestContext testContext) : IClassFixture<GameboardTestContext>
{
    private readonly GameboardTestContext _testContext = testContext;

    [Theory, GbIntegrationAutoData]
    public async Task Grade_WithFirstSolve_SetsExpectedRankAndScore
    (
        string challengeId,
        string challengeSpecId,
        string gameId,
        string graderKey,
        string teamId,
        IFixture fixture
    )
    {
        // given a team with a standard challenge spec scoring for the first time
        // note: we still have to mock the grading stuff because we're not testing with a real engine
        await _testContext
            .WithDataState(state =>
            {
                state.Add<Data.Game>(fixture, g =>
                {
                    g.Id = gameId;
                    g.Specs = state.Build<Data.ChallengeSpec>(fixture, spec =>
                    {
                        spec.Id = challengeSpecId;
                        spec.Points = 100;
                    }).ToCollection();

                    g.Challenges = state.Build<Data.Challenge>(fixture, c =>
                    {
                        c.Id = challengeId;
                        c.GraderKey = graderKey.ToSha256();
                        c.Points = 0;
                        c.Score = 0;
                        c.SpecId = challengeSpecId;
                        c.TeamId = teamId;
                        c.Player = state.Build<Data.Player>(fixture, p =>
                        {
                            p.GameId = gameId;
                            p.Score = 0;
                            p.SessionBegin = DateTimeOffset.UtcNow;
                            p.TeamId = teamId;
                        });
                    }).ToCollection();
                });
            });

        // when they score
        await _testContext
            .CreateHttpClientWithGraderConfig(100, graderKey)
            .PutAsync("/api/challenge/grade", new GameEngineSectionSubmission
            {
                Id = challengeId,
                Timestamp = DateTimeOffset.UtcNow,
                SectionIndex = 0,
                Questions = new GameEngineAnswerSubmission { Answer = "test" }.ToEnumerable()
            }.ToJsonBody())
            .DeserializeResponseAs<Challenge>();

        // then the players table should have the expected properties set
        await _testContext.ValidateStoreStateAsync(async dbContext =>
        {
            var teamRanking = await dbContext
                .DenormalizedTeamScores
                .AsNoTracking()
                .Where(t => t.TeamId == teamId)
                .SingleAsync();

            teamRanking.Rank.ShouldBe(1);
            teamRanking.ScoreOverall.ShouldBe(100);

            // and also the challenge should have a grading event
            var events = await dbContext
                .ChallengeEvents
                .AsNoTracking()
                .Where(e => e.ChallengeId == challengeId && e.Type == ChallengeEventType.Submission)
                .ToArrayAsync();

            events.Length.ShouldBe(1);
        });
    }

    [Theory, GbIntegrationAutoData]
    public async Task Grade_WithGamespaceExpired_ThrowsAndLogsEvent
    (
        string gameId,
        string challengeId,
        string challengeSpecId,
        string challengeGraderKey,
        string sponsorId,
        string teamId,
        string userId,
        IFixture fixture
    )
    {
        var now = DateTimeOffset.UtcNow;
        var challengeStartTime = now.AddDays(-1);
        var challengeEndTime = now.AddMinutes(-5);

        // given a challenge and a faked game engine service
        // which will throw an expired exception on grade
        await _testContext.WithDataState(state =>
        {
            state.Add<Data.Sponsor>(fixture, s => s.Id = sponsorId);
            state.Add<Data.ChallengeSpec>(fixture, s => s.Id = challengeSpecId);
            state.Add<Data.Game>(fixture, g =>
            {
                g.Id = gameId;
                g.Players =
                [
                    state.Build<Data.Player>(fixture, p =>
                    {
                        p.Id = fixture.Create<string>();
                        p.Challenges =
                        [
                            new()
                            {
                                Id = challengeId,
                                EndTime = challengeEndTime,
                                StartTime = challengeStartTime,
                                SpecId = challengeSpecId,
                                GameId = gameId,
                                GraderKey = challengeGraderKey.ToSha256(),
                                TeamId = teamId
                            }
                        ];
                        p.SponsorId = sponsorId;
                        p.TeamId = teamId;
                        p.User = state.Build<Data.User>(fixture, u => u.Id = userId);
                    })
                ];
            });
        });

        var submission = new GameEngineSectionSubmission
        {
            Id = challengeId,
            SectionIndex = 0,
            Timestamp = DateTimeOffset.UtcNow,
            Questions = []
        };

        await _testContext
            .BuildTestApplication(u => u.Id = userId, services =>
            {
                var testGradingResultService = new TestGradingResultService(new TestGradingResultServiceConfiguration
                {
                    ThrowsOnGrading = new SubmissionIsForExpiredGamespace(challengeId, new GameboardIntegrationTestException("Expected exception"))
                });

                services.ReplaceService<ITestGradingResultService, TestGradingResultService>(testGradingResultService);
            })
            .CreateClient()
            .PutAsync("/api/challenge/grade", submission.ToJsonBody());

        await _testContext.ValidateStoreStateAsync(async dbContext =>
        {
            var challengeEvents = await dbContext
                .ChallengeEvents
                .AsNoTracking()
                .Where(ev => ev.ChallengeId == challengeId)
                .Where(ev => ev.Type == ChallengeEventType.SubmissionRejectedGamespaceExpired)
                .OrderByDescending(ev => ev.Timestamp)
                .ToArrayAsync();

            challengeEvents.Length.ShouldBe(1);
        });

    }

    [Theory, GbIntegrationAutoData]
    public async Task Grade_ExecutionTimeOver_ThrowsAndLogsEvent
    (
        string gameId,
        string graderKey,
        string challengeId,
        string challengeSpecId,
        string sponsorId,
        string teamId,
        string userId,
        IFixture fixture
    )
    {
        var now = DateTime.UtcNow;
        var challengeStartTime = now.AddDays(-1);
        var challengeEndTime = now.AddDays(1);
        var gameStartTime = now.AddDays(-1);
        var gameEndTime = now.AddMinutes(-1);

        // given a challenge and a faked game engine service
        // which will throw an expired exception on grade
        await _testContext.WithDataState(state =>
        {
            state.Add<Data.Sponsor>(fixture, s => s.Id = sponsorId);
            state.Add<Data.ChallengeSpec>(fixture, s => s.Id = challengeSpecId);
            state.Add<Data.Game>(fixture, g =>
            {
                g.Id = gameId;
                g.GameStart = gameStartTime;
                g.GameEnd = gameEndTime;
                g.PlayerMode = PlayerMode.Competition;
                g.Players =
                [
                    new()
                    {
                        Id = fixture.Create<string>(),
                        Challenges =
                        [
                            new()
                            {
                                Id = challengeId,
                                EndTime = challengeEndTime,
                                GraderKey = graderKey.ToSha256(),
                                StartTime = challengeStartTime,
                                SpecId = challengeSpecId,
                                GameId = gameId,
                                TeamId = teamId
                            }
                        ],
                        SponsorId = sponsorId,
                        TeamId = teamId,
                        User = state.Build<Data.User>(fixture, u => u.Id = userId)
                    }
                ];
            });
        });

        var submission = new GameEngineSectionSubmission
        {
            Id = challengeId,
            SectionIndex = 0,
            Timestamp = now,
            Questions = []
        };

        var http = _testContext
            .CreateHttpClientWithGraderConfig(100, graderKey);

        // Under the hood, the API does throw a specialized exception for this, but we only get 400s for now.
        var result = await http.PutAsync("/api/challenge/grade", submission.ToJsonBody());
        result.IsSuccessStatusCode.ShouldBeFalse();

        await _testContext.ValidateStoreStateAsync(async db =>
        {
            var challengeEvents = await db
                .ChallengeEvents
                .AsNoTracking()
                .Where(ev => ev.ChallengeId == challengeId)
                .Where(ev => ev.Type == ChallengeEventType.SubmissionRejectedGameEnded)
                .OrderByDescending(ev => ev.Timestamp)
                .ToArrayAsync();

            challengeEvents.Length.ShouldBe(1);
        });
    }
}
