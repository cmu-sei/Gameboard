using Gameboard.Api.Common;
using Gameboard.Api.Features.GameEngine;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Tests.Integration;

public class ChallengeControllerGradeTests : IClassFixture<GameboardTestContext>
{
    private readonly GameboardTestContext _testContext;

    public ChallengeControllerGradeTests(GameboardTestContext testContext)
        => _testContext = testContext;

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
                            p.Rank = 0;
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
        var player = await _testContext
            .GetDbContext()
            .Players
            .AsNoTracking()
            .Where(p => p.TeamId == teamId)
            .SingleAsync();

        player.Rank.ShouldBe(1);
        player.Score.ShouldBe(100);

        // and also the challenge should have a grading event
        var events = await _testContext
            .GetDbContext()
            .ChallengeEvents
            .AsNoTracking()
            .Where(e => e.ChallengeId == challengeId && e.Type == ChallengeEventType.Submission)
            .ToArrayAsync();

        events.Length.ShouldBe(1);
    }

    [Theory, GbIntegrationAutoData]
    public async Task Grade_WithGamespaceExpired_ThrowsAndLogsEvent
    (
        string gameId,
        string challengeId,
        string challengeSpecId,
        string sponsorId,
        string userId,
        IFixture fixture
    )
    {
        var challengeStartTime = DateTime.UtcNow.AddDays(-1);
        var challengeEndTime = DateTime.UtcNow.AddMinutes(-5);

        // given a challenge and a faked game engine service
        // which will throw an expired exception on grade
        await _testContext.WithDataState(state =>
        {
            state.Add<Data.Sponsor>(fixture, s => s.Id = sponsorId);
            state.Add<Data.Game>(fixture, g =>
            {
                g.Id = gameId;
                g.Players = new List<Data.Player>
                {
                    new()
                    {
                        Id = fixture.Create<string>(),
                        Challenges = new List<Data.Challenge>
                        {
                            new()
                            {
                                Id = challengeId,
                                EndTime = challengeEndTime,
                                StartTime = challengeStartTime,
                                SpecId = challengeSpecId,
                                GameId = gameId
                            }
                        },
                        SponsorId = sponsorId,
                        User = state.Build<Data.User>(fixture, u => u.Id = userId)
                    }
                };
            });
        });

        var exceptionToThrow = new SubmissionIsForExpiredGamespace(challengeId, null);
        var submission = new GameEngineSectionSubmission
        {
            Id = challengeId,
            SectionIndex = 0,
            Timestamp = DateTimeOffset.UtcNow,
            Questions = Array.Empty<GameEngineAnswerSubmission>()
        };

        var http = _testContext
            .BuildTestApplication(u => u.Id = userId, services =>
            {
                var gradingResultConfig = new TestGradingResultServiceConfiguration { ThrowsOnGrading = exceptionToThrow };
                services.ReplaceService<ITestGradingResultService, TestGradingResultService>(new TestGradingResultService(gradingResultConfig));
            })
            .CreateClient();

        await http.PutAsync("/api/challenge/grade", submission.ToJsonBody());

        var challengeEvents = await _testContext
            .GetDbContext()
            .ChallengeEvents
            .AsNoTracking()
            .Where(ev => ev.ChallengeId == challengeId)
            .Where(ev => ev.Type == ChallengeEventType.SubmissionRejectedGamespaceExpired)
            .OrderByDescending(ev => ev.Timestamp)
            .ToArrayAsync();

        challengeEvents.Length.ShouldBe(1);
    }

    [Theory, GbIntegrationAutoData]
    public async Task Grade_ExecutionTimeOver_ThrowsAndLogsEvent
    (
        string gameId,
        string graderKey,
        string challengeId,
        string challengeSpecId,
        string sponsorId,
        string userId,
        IFixture fixture
    )
    {
        var challengeStartTime = DateTime.UtcNow.AddDays(-1);
        var challengeEndTime = DateTime.UtcNow.AddDays(1);
        var gameStartTime = DateTime.UtcNow.AddDays(-1);
        var gameEndTime = DateTime.UtcNow.AddMinutes(-1);

        // given a challenge and a faked game engine service
        // which will throw an expired exception on grade
        await _testContext.WithDataState(state =>
        {
            state.Add<Data.Sponsor>(fixture, s => s.Id = sponsorId);
            state.Add<Data.Game>(fixture, g =>
            {
                g.Id = gameId;
                g.GameStart = gameStartTime;
                g.GameEnd = gameEndTime;
                g.PlayerMode = PlayerMode.Competition;
                g.Players = new List<Data.Player>
                {
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
                                GameId = gameId
                            }
                        ],
                        SponsorId = sponsorId,
                        User = state.Build<Data.User>(fixture, u => u.Id = userId)
                    }
                };
            });
        });

        var submission = new GameEngineSectionSubmission
        {
            Id = challengeId,
            SectionIndex = 0,
            Timestamp = DateTimeOffset.UtcNow,
            Questions = Array.Empty<GameEngineAnswerSubmission>()
        };

        var http = _testContext
            .CreateHttpClientWithGraderConfig(100, graderKey);

        // Under the hood, the API does throw a specialized exception for this, but we only get 400s for now.
        // await Should.ThrowAsync<CantGradeBecauseGameExecutionPeriodIsOver>(() => http.PutAsync("/api/challenge/grade", submission.ToJsonBody()));
        var result = await http.PutAsync("/api/challenge/grade", submission.ToJsonBody());
        result.IsSuccessStatusCode.ShouldBeFalse();

        var challengeEvents = await _testContext
            .GetDbContext()
            .ChallengeEvents
            .AsNoTracking()
            .Where(ev => ev.ChallengeId == challengeId)
            .Where(ev => ev.Type == ChallengeEventType.SubmissionRejectedGameEnded)
            .OrderByDescending(ev => ev.Timestamp)
            .ToArrayAsync();

        challengeEvents.Length.ShouldBe(1);
    }
}
