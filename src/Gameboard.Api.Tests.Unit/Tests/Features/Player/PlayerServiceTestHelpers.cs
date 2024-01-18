using AutoMapper;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Data;
using Gameboard.Api.Data.Abstractions;
using Gameboard.Api.Features.Games;
using Gameboard.Api.Features.Practice;
using Gameboard.Api.Features.Teams;
using Gameboard.Api.Services;
using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Gameboard.Api.Tests.Unit;

internal static class PlayerServiceTestHelpers
{
    // TODO: reflection helper
    public static PlayerService GetTestableSut
    (
        ChallengeService? challengeService = null,
        CoreOptions? coreOptions = null,
        IGameStore? gameStore = null,
        IGuidService? guidService = null,
        IInternalHubBus? hubBus = null,
        ILogger<PlayerService>? logger = null,
        IMapper? mapper = null,
        IMemoryCache? memCache = null,
        INowService? now = null,
        IPlayerStore? playerStore = null,
        IPracticeService? practiceService = null,
        IStore? store = null,
        ISyncStartGameService? syncStartGameService = null,
        ITeamService? teamService = null
    )
    {
        return new PlayerService
        (
            challengeService ?? A.Fake<ChallengeService>(),
            coreOptions ?? A.Fake<CoreOptions>(),
            gameStore ?? A.Fake<IGameStore>(),
            guidService ?? A.Fake<IGuidService>(),
            hubBus ?? A.Fake<IInternalHubBus>(),
            logger ?? A.Fake<ILogger<PlayerService>>(),
            mapper ?? A.Fake<IMapper>(),
            memCache ?? A.Fake<IMemoryCache>(),
            now ?? A.Fake<INowService>(),
            playerStore ?? A.Fake<IPlayerStore>(),
            practiceService ?? A.Fake<IPracticeService>(),
            store ?? A.Fake<IStore>(),
            syncStartGameService ?? A.Fake<ISyncStartGameService>(),
            teamService ?? A.Fake<ITeamService>()
        );
    }
}
