using AutoMapper;
using Gameboard.Api.Data;
using Gameboard.Api.Data.Abstractions;
using Gameboard.Api.Features.Practice;
using Gameboard.Api.Features.Teams;
using Gameboard.Api.Hubs;
using Gameboard.Api.Services;
using Microsoft.Extensions.Caching.Memory;

namespace Gameboard.Api.Tests.Unit;

internal static class PlayerServiceTestHelpers
{
    // TODO: reflection helper
    public static PlayerService GetTestableSut
    (
        CoreOptions? coreOptions = null,
        ChallengeService? challengeService = null,
        IPlayerStore? playerStore = null,
        IGameService? gameService = null,
        IGameStore? gameStore = null,
        IGuidService? guidService = null,
        INowService? now = null,
        IInternalHubBus? hubBus = null,
        IPracticeChallengeScoringListener? practiceChallengeScoringListener = null,
        IPracticeService? practiceService = null,
        ITeamService? teamService = null,
        IMapper? mapper = null,
        IMemoryCache? localCache = null
    )
    {
        return new PlayerService
        (
            challengeService ?? A.Fake<ChallengeService>(),
            coreOptions ?? A.Fake<CoreOptions>(),
            guidService ?? A.Fake<IGuidService>(),
            now ?? A.Fake<INowService>(),
            playerStore ?? A.Fake<IPlayerStore>(),
            gameService ?? A.Fake<GameService>(),
            gameStore ?? A.Fake<IGameStore>(),
            hubBus ?? A.Fake<IInternalHubBus>(),
            practiceChallengeScoringListener ?? A.Fake<IPracticeChallengeScoringListener>(),
            practiceService ?? A.Fake<IPracticeService>(),
            teamService ?? A.Fake<ITeamService>(),
            mapper ?? A.Fake<IMapper>(),
            localCache ?? A.Fake<IMemoryCache>()
        );
    }
}
