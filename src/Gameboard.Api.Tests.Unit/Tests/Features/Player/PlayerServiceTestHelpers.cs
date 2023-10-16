using AutoMapper;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Data;
using Gameboard.Api.Data.Abstractions;
using Gameboard.Api.Features.GameEngine;
using Gameboard.Api.Features.Games;
using Gameboard.Api.Features.Games.Start;
using Gameboard.Api.Features.Practice;
using Gameboard.Api.Features.Teams;
using Gameboard.Api.Services;
using MediatR;
using Microsoft.Extensions.Caching.Memory;

namespace Gameboard.Api.Tests.Unit;

internal static class PlayerServiceTestHelpers
{
    // TODO: reflection helper
    public static PlayerService GetTestableSut
    (
        ChallengeService? challengeService = null,
        CoreOptions? coreOptions = null,
        IGameEngineService? gameEngine = null,
        IGameStartService? gameStartService = null,
        IGameStore? gameStore = null,
        IGuidService? guidService = null,
        IInternalHubBus? hubBus = null,
        IMapper? mapper = null,
        IMemoryCache? memCache = null,
        INowService? now = null,
        IPlayerStore? playerStore = null,
        IPracticeChallengeEventsListener? practiceChallengeScoringListener = null,
        IPracticeService? practiceService = null,
        IStore? store = null,
        ITeamService? teamService = null
    )
    {
        return new PlayerService
        (
            challengeService ?? A.Fake<ChallengeService>(),
            coreOptions ?? A.Fake<CoreOptions>(),
            gameEngine ?? A.Fake<IGameEngineService>(),
            gameStartService ?? A.Fake<IGameStartService>(),
            gameStore ?? A.Fake<IGameStore>(),
            guidService ?? A.Fake<IGuidService>(),
            hubBus ?? A.Fake<IInternalHubBus>(),
            mapper ?? A.Fake<IMapper>(),
            memCache ?? A.Fake<IMemoryCache>(),
            now ?? A.Fake<INowService>(),
            playerStore ?? A.Fake<IPlayerStore>(),
            practiceChallengeScoringListener ?? A.Fake<IPracticeChallengeEventsListener>(),
            practiceService ?? A.Fake<IPracticeService>(),
            store ?? A.Fake<IStore>(),
            teamService ?? A.Fake<ITeamService>()
        );
    }
}
