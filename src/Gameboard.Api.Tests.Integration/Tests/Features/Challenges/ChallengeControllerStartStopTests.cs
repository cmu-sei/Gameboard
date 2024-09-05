using Gameboard.Api.Features.GameEngine;

namespace Gameboard.Api.Tests.Integration;

/// <summary>
/// The main point of these tests is to preserve rules about how the HasDeployedGamespace property
/// of the Challenge entity is computed.
/// 
/// In general, Gameboard does its best to update entries in its Challenge table with data given
/// to it by the underlying game engine (currently this is Topomojo in all production cases). When
/// A Gameboard user performs an operation causing a game engine change, it listens for the state
/// coming back from the game engine and updates the Challenge entity with data from it. For example,
/// when a Gameboard users Stops the gamespace, Gameboard asks the engine to stop the gamespace and then
/// updates the challenge to reflect its stopped state.
/// 
/// Topomojo represents Stopped challenges as gamespaces with no VMs. Notably, the IsActive property
/// of the state DOES NOT REFLECT the stoppedness of the gamespace. Thus, we have these tests to verify
/// that we're using the VM count NOT the IsActive flag as the basis for Gameboard's decision.
/// </summary>
public class ChallengeControllerStartStopTests : IClassFixture<GameboardTestContext>
{
    private readonly GameboardTestContext _testContext;

    public ChallengeControllerStartStopTests(GameboardTestContext testContext)
    {
        _testContext = testContext;
    }

    [Theory, GbIntegrationAutoData]
    public async Task Start_WithGameEngineResponseWithVMsAndNotIsActive_HasDeployedGamespace
    (
        string challengeId,
        string gameId,
        string playerId,
        string sponsorId,
        string teamId,
        string userId,
        IFixture fixture
    )
    {
        // given a challenge and a game engine service that will return a gamespace
        // with IsActive = false and a non-empty list of VMs when asked to Start 
        var gameEngineStateChangeService = new TestGameEngineStateChangeService(startGamespaceResult: new GameEngineGameState
        {
            Id = challengeId,
            // the main gag here is that, by rule, IsActive DOES NOT relate to HasDeployedGamespace, at least for Topomojo, 
            // which is our only actively used game engine. HasDeployedGamespace is true if the state returned from the
            // engine has a nonzero number of VMs.
            IsActive = false,
            Vms = new GameEngineVmState[]
            {
                new() { Name = fixture.Create<string>() }
            }
        });

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
                        Id = playerId,
                        SponsorId = sponsorId,
                        TeamId = teamId,
                        User = state.Build<Data.User>(fixture, u => u.Id = userId)
                    }
                };
            });

            state.Add<Data.Challenge>(fixture, c =>
            {
                c.Id = challengeId;
                c.GameId = gameId;
                c.PlayerId = playerId;
                c.TeamId = teamId;
            });
        });

        var http = _testContext
            .BuildTestApplication(u => u.Id = userId, services =>
            {
                services.ReplaceService<ITestGameEngineStateChangeService, TestGameEngineStateChangeService>(gameEngineStateChangeService);
            })
            .CreateClient();

        // when the challenge is started
        var result = await http
            .PutAsync("/api/challenge/start", new ChangedChallenge { Id = challengeId }.ToJsonBody())
            .DeserializeResponseAs<Challenge>();

        // then the resulting challenge should have a deployed gamespace
        result.HasDeployedGamespace.ShouldBeTrue();
    }

    [Theory, GbIntegrationAutoData]
    public async Task Stop_WithGameEngineResponseNoVMsAndIsActive_DoesNotHaveDeployedGamespace
    (
        string challengeId,
        string gameId,
        string playerId,
        string sponsorId,
        string teamId,
        string userId,
        IFixture fixture
    )
    {
        // given a challenge and a game engine service that will return a gamespace
        // with IsActive = true and an empty list of VMs when asked to Stop 
        var gameEngineStateChangeService = new TestGameEngineStateChangeService(stopGamespaceResult: new GameEngineGameState
        {
            Id = challengeId,
            IsActive = true,
            Vms = Array.Empty<GameEngineVmState>()
        });

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
                        Id = playerId,
                        SponsorId = sponsorId,
                        TeamId = teamId,
                        User = state.Build<Data.User>(fixture, u => u.Id = userId)
                    }
                };
            });

            state.Add<Data.Challenge>(fixture, c =>
            {
                c.Id = challengeId;
                c.GameId = gameId;
                c.PlayerId = playerId;
                c.TeamId = teamId;
            });
        });

        var http = _testContext
            .BuildTestApplication(u => u.Id = userId, services =>
            {
                services.ReplaceService<ITestGameEngineStateChangeService, TestGameEngineStateChangeService>(gameEngineStateChangeService);
            })
            .CreateClient();

        // when the challenge is stopped
        var result = await http
            .PutAsync("/api/challenge/stop", new ChangedChallenge { Id = challengeId }.ToJsonBody())
            .DeserializeResponseAs<Challenge>();

        // then the resulting challenge should have a deployed gamespace
        result.HasDeployedGamespace.ShouldBeFalse();
    }
}
