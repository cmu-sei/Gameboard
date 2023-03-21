using AutoMapper;
using Gameboard.Api;
using Gameboard.Api.Data.Abstractions;
using Gameboard.Api.Features.GameEngine;
using Gameboard.Api.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Gameboard.Tests.Unit;

public class ChallengeServiceTests
{
    /// <summary>
    /// When a challenge is created, its ID should match the gamespaceId returned by the game engine.
    /// </summary>
    /// <param name="gameId"></param>
    /// <param name="playerId"></param>
    /// <param name="gamespaceId"></param>
    /// <param name="graderUrl"></param>
    /// <param name="specId"></param>
    /// <param name="specExternalId"></param>
    /// <param name="teamId"></param>
    /// <param name="userId"></param>
    [Theory, GameboardAutoData]
    public async Task BuildAndRegister_WithRequiredData_UsesTopoGamespaceIdAsChallengeId
    (
        string gameId,
        string playerId,
        string gamespaceId,
        string graderKey,
        string graderUrl,
        string specId,
        string specExternalId,
        string teamId,
        string userId
    )
    {
        // given 
        var newChallenge = new NewChallenge
        {
            PlayerId = playerId,
            SpecId = specId
        };

        var fakePlayer = new Api.Data.Player
        {
            Id = playerId,
            GameId = gameId,
            TeamId = teamId
        };

        var fakeGame = new Api.Data.Game
        {
            Id = gameId,
            MaxTeamSize = 1,
            Prerequisites = new Api.Data.ChallengeGate[] { }
        };
        var fakeGames = new Api.Data.Game[] { fakeGame }.BuildMock();

        var fakeSpec = new Api.Data.ChallengeSpec
        {
            Id = specId,
            ExternalId = specExternalId
        };

        var fakeGameEngineService = A.Fake<IGameEngineService>();
        A
            .CallTo(() => fakeGameEngineService.RegisterGamespace
            (
                new GameEngineChallengeRegistration
                {
                    Challenge = new Api.Data.Challenge { },
                    ChallengeSpec = fakeSpec,
                    Game = fakeGame,
                    Player = fakePlayer,
                    GraderKey = graderKey,
                    GraderUrl = graderUrl,
                    PlayerCount = 1,
                    Variant = 0
                }
            ))
            .WithAnyArguments()
            .Returns
            (
                new GameEngineGameState
                {
                    Id = gamespaceId,
                    IsActive = true,
                    StartTime = DateTimeOffset.Now,
                    EndTime = DateTimeOffset.Now.AddDays(1),
                }
            );

        var sut = new ChallengeService
        (
            A.Fake<ILogger<ChallengeService>>(),
            A.Fake<IMapper>(),
            A.Fake<CoreOptions>(),
            A.Fake<IChallengeStore>(),
            A.Fake<IChallengeSpecStore>(),
            fakeGameEngineService,
            A.Fake<IGameStore>(),
            A.Fake<IGuidService>(),
            A.Fake<IJsonService>(),
            A.Fake<IMemoryCache>(),
            A.Fake<INowService>(),
            A.Fake<IPlayerStore>(),
            A.Fake<ConsoleActorMap>()
        );

        // when
        var result = await sut.BuildAndRegisterChallenge
        (
            newChallenge,
            fakeSpec,
            fakeGame,
            fakePlayer,
            userId,
            graderUrl,
            1,
            0
        );

        // then
        result.Id.ShouldBe(gamespaceId);
    }
}
