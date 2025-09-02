using AutoMapper;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Challenges;
using Gameboard.Api.Features.GameEngine;
using Gameboard.Api.Features.Practice;
using Gameboard.Api.Features.Teams;
using Gameboard.Api.Features.Users;
using Gameboard.Api.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Gameboard.Api.Tests.Unit;

public class ChallengeServiceTests
{
    /// <summary>
    /// When a challenge is created, its ID should match the gamespaceId returned by the game engine.
    /// </summary>
    /// <param name="gameId"></param>
    /// <param name="playerId"></param>
    /// <param name="gamespaceId"></param>
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

        var fakePlayer = new Data.Player
        {
            Id = playerId,
            GameId = gameId,
            TeamId = teamId
        };

        var fakeGame = new Data.Game
        {
            Id = gameId,
            MaxTeamSize = 1,
            Prerequisites = Array.Empty<Data.ChallengeGate>()
        };
        var fakeGames = new Data.Game[] { fakeGame }.BuildMock();

        var fakeSpec = new Data.ChallengeSpec
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
                    AttemptLimit = 0,
                    Challenge = new Data.Challenge { SpecId = specId },
                    ChallengeSpec = fakeSpec,
                    Game = fakeGame,
                    Player = fakePlayer,
                    GraderKey = graderKey,
                    GraderUrl = graderUrl,
                    PlayerCount = 1,
                    StartGamespace = false,
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
                    StartTime = DateTimeOffset.UtcNow,
                    EndTime = DateTimeOffset.UtcNow.AddDays(1),
                }
            );

        var sut = new ChallengeService
        (
            A.Fake<IActingUserService>(),
            A.Fake<ConsoleActorMap>(),
            A.Fake<CoreOptions>(),
            A.Fake<IChallengeGraderUrlService>(),
            A.Fake<IChallengeStore>(),
            A.Fake<IChallengeDocsService>(),
            A.Fake<IChallengeSubmissionsService>(),
            A.Fake<IChallengeSyncService>(),
            fakeGameEngineService,
            A.Fake<IGuidService>(),
            A.Fake<IJsonService>(),
            A.Fake<ILogger<ChallengeService>>(),
            A.Fake<IMapper>(),
            A.Fake<IMediator>(),
            A.Fake<INowService>(),
            A.Fake<IPracticeService>(),
            A.Fake<IUserRolePermissionsService>(),
            A.Fake<IStore>(),
            A.Fake<ITeamService>()
        );

        // when
        var result = await sut.BuildAndRegisterChallenge
        (
            newChallenge,
            fakeSpec,
            fakeGame,
            fakePlayer,
            userId,
            1,
            0
        );

        // then
        result.Id.ShouldBe(gamespaceId);
    }

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
    public async Task BuildAndRegister_WithGamePlayerModePractice_ShouldProducePracticeModeChallenge
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

        var fakeGame = new Data.Game
        {
            Id = gameId,
            MaxTeamSize = 1,
            PlayerMode = PlayerMode.Practice,
            Prerequisites = Array.Empty<Data.ChallengeGate>()
        };
        var fakeGames = new Data.Game[] { fakeGame }.BuildMock();

        var fakeSpec = new Data.ChallengeSpec
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
                    AttemptLimit = 0,
                    Challenge = new Data.Challenge { SpecId = specId },
                    ChallengeSpec = fakeSpec,
                    Game = fakeGame,
                    Player = fakePlayer,
                    GraderKey = graderKey,
                    GraderUrl = graderUrl,
                    PlayerCount = 1,
                    StartGamespace = false,
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
                    StartTime = DateTimeOffset.UtcNow,
                    EndTime = DateTimeOffset.UtcNow.AddDays(1),
                }
            );

        var sut = new ChallengeService
        (
            A.Fake<IActingUserService>(),
            A.Fake<ConsoleActorMap>(),
            A.Fake<CoreOptions>(),
            A.Fake<IChallengeGraderUrlService>(),
            A.Fake<IChallengeStore>(),
            A.Fake<IChallengeDocsService>(),
            A.Fake<IChallengeSubmissionsService>(),
            A.Fake<IChallengeSyncService>(),
            fakeGameEngineService,
            A.Fake<IGuidService>(),
            A.Fake<IJsonService>(),
            A.Fake<ILogger<ChallengeService>>(),
            A.Fake<IMapper>(),
            A.Fake<IMediator>(),
            A.Fake<INowService>(),
            A.Fake<IPracticeService>(),
            A.Fake<IUserRolePermissionsService>(),
            A.Fake<IStore>(),
            A.Fake<ITeamService>()
        );

        // when
        var result = await sut.BuildAndRegisterChallenge
        (
            newChallenge,
            fakeSpec,
            fakeGame,
            fakePlayer,
            userId,
            1,
            0
        );

        // then
        result.PlayerMode.ShouldBe(PlayerMode.Practice);
    }
}
