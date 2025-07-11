using Gameboard.Api.Common;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Consoles;
using Gameboard.Api.Features.GameEngine;

namespace Gameboard.Api.Tests.Integration;

public class RecordUserConsoleActiveTests(GameboardTestContext testContext) : IClassFixture<GameboardTestContext>
{
    private readonly GameboardTestContext _testContext = testContext;

    [Theory, GbIntegrationAutoData]
    public async Task ActionRecorded_WithPracticeSessionNearEnd_Extends(string challengeId, string userId, IFixture fixture)
    {
        // given
        await _testContext.WithDataState(state =>
        {
            state.Add<Data.Player>(fixture, p =>
            {
                p.SessionBegin = DateTimeOffset.UtcNow.AddMinutes(-10);
                p.SessionEnd = DateTimeOffset.UtcNow.AddMinutes(5);
                p.Sponsor = state.Build<Data.Sponsor>(fixture);
                p.Mode = PlayerMode.Practice;
                p.User = state.Build<Data.User>(fixture, u => u.Id = userId);
                p.Challenges = state.Build<Data.Challenge>(fixture, c =>
                {
                    c.Id = challengeId;
                    c.PlayerMode = PlayerMode.Practice;
                    c.State = GetChallengeState(challengeId);
                }).ToCollection();
            });
        });

        // when
        var result = await _testContext
            .CreateHttpClientWithActingUser(u => u.Id = userId)
            .PostAsync("api/consoles/active", new ConsoleId { ChallengeId = challengeId, Name = "my-vm" }.ToJsonBody())
            .DeserializeResponseAs<ConsoleActionResponse>();

        // then
        // (See the source of RecordUserConsoleActive for a discussion about why this is currently a string)
        result.Message.ShouldContain(RecordUserConsoleActiveHandler.MESSAGE_EXTENDED);
    }

    [Theory, GbIntegrationAutoData]
    public async Task ActionRecorded_WithPracticeSessionNotNearEnd_DoesNotExtend(string challengeId, string userId, IFixture fixture)
    {
        // given
        await _testContext.WithDataState(state =>
        {
            state.Add<Data.Player>(fixture, p =>
            {
                p.SessionBegin = DateTimeOffset.UtcNow.AddMinutes(-10);
                // since the session end is a ways out, we don't expect the auto-extend here
                p.SessionEnd = DateTimeOffset.UtcNow.AddMinutes(120);
                p.Sponsor = state.Build<Data.Sponsor>(fixture);
                p.Mode = PlayerMode.Practice;
                p.User = state.Build<Data.User>(fixture, u => u.Id = userId);
                p.Challenges = state.Build<Data.Challenge>(fixture, c =>
                {
                    c.Id = challengeId;
                    c.PlayerMode = PlayerMode.Practice;
                    c.State = GetChallengeState(challengeId);
                }).ToCollection();
            });
        });

        // when
        var result = await _testContext
            .CreateHttpClientWithActingUser(u => u.Id = userId)
            .PostAsync("api/consoles/active", new ConsoleId { ChallengeId = challengeId, Name = "my-vm" }.ToJsonBody())
            .DeserializeResponseAs<ConsoleActionResponse>();

        // then
        // (See the source of RecordUserConsoleActive for a discussion about why this is currently a string)
        result.Message.ShouldBe(RecordUserConsoleActiveHandler.MESSAGE_NOT_EXTENDED);
    }

    [Theory, GbIntegrationAutoData]
    public async Task ActionRecorded_WithCompetitiveSessionNearEnd_Extends(string challengeId, string userId, IFixture fixture)
    {
        // given
        await _testContext.WithDataState(state =>
        {
            state.Add<Data.Player>(fixture, p =>
            {
                p.SessionBegin = DateTimeOffset.UtcNow.AddMinutes(-10);
                p.SessionEnd = DateTimeOffset.UtcNow.AddMinutes(30);
                p.Sponsor = state.Build<Data.Sponsor>(fixture);
                // since this is a competitive challenge, it shouldn't be auto-extended
                p.Mode = PlayerMode.Competition;
                p.User = state.Build<Data.User>(fixture, u => u.Id = userId);
                p.Challenges = state.Build<Data.Challenge>(fixture, c =>
                {
                    c.Id = challengeId;
                    c.PlayerMode = PlayerMode.Practice;
                    c.State = GetChallengeState(challengeId);
                }).ToCollection();
            });
        });

        // when
        var result = await _testContext
            .CreateHttpClientWithActingUser(u => u.Id = userId)
            .PostAsync("api/consoles/active", new ConsoleId { ChallengeId = challengeId, Name = "my-vm" }.ToJsonBody())
            .DeserializeResponseAs<ConsoleActionResponse>();

        // then
        result.Message.ShouldBeNull();
    }

    private string GetChallengeState(string challengeId)
    {
        var state = new GameEngineGameState
        {
            Vms = [new GameEngineVmState
            {
                Id = "123",
                Name = "my-vm",
                IsolationId = challengeId,
                IsRunning = true,
                IsVisible = true
            }]
        };

        var jsonService = _testContext.Services.GetRequiredService<IJsonService>();
        return jsonService.Serialize(state);
    }
}
