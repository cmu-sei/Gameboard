using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Gameboard.Api.Common;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Data.Abstractions;
using Gameboard.Api.Features.Games.External;
using Gameboard.Api.Features.Teams;
using Gameboard.Api.Structure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Gameboard.Api.Features.Games.Start;

public interface IGameStartService
{
    Task<GameStartPhase> GetGameStartPhase(string gameId);
    Task HandleSyncStartStateChanged(string gameId);
    Task<GameStartState> Start(GameStartRequest request);
}

internal class GameStartService : IGameStartService
{
    private readonly IStore<Data.ChallengeSpec> _challengeSpecStore;
    private readonly IGameHubBus _gameHubBus;
    private readonly IGameStore _gameStore;
    private readonly ILogger<GameStartService> _logger;
    private readonly IMapper _mapper;
    private readonly INowService _now;
    private readonly IPlayerStore _playerStore;
    private readonly IExternalSyncGameStartService _externalSyncGameStartService;
    private readonly ISyncStartGameService _syncStartGameService;
    private readonly ITeamService _teamService;

    public GameStartService
    (
        IStore<Data.ChallengeSpec> challengeSpecStore,
        IExternalSyncGameStartService externalSyncGameStartService,
        IGameHubBus gameHubBus,
        IGameStore gameStore,
        ILogger<GameStartService> logger,
        IMapper mapper,
        INowService now,
        IPlayerStore playerStore,
        ISyncStartGameService syncGameStartService,
        ITeamService teamService
    )
    {
        _challengeSpecStore = challengeSpecStore;
        _externalSyncGameStartService = externalSyncGameStartService;
        _gameHubBus = gameHubBus;
        _gameStore = gameStore;
        _logger = logger;
        _mapper = mapper;
        _now = now;
        _playerStore = playerStore;
        _syncStartGameService = syncGameStartService;
        _teamService = teamService;
    }

    public async Task<GameStartState> Start(GameStartRequest request)
    {
        var game = await _gameStore.Retrieve(request.GameId);
        var gameModeStartService = ResolveGameModeStartService(game) ?? throw new NotImplementedException();
        var startRequest = await LoadGameModeStartRequest(game);

        try
        {
            return await gameModeStartService.Start(startRequest);
        }
        catch (Exception ex)
        {
            _logger.LogError(LogEventId.GameStart_Failed, exception: ex, message: $"""Deploy for game "{game.Id}" """);
            await TryCleanupFailedDeploy(startRequest.State);
        }

        return null;
    }

    public async Task<GameStartPhase> GetGameStartPhase(string gameId)
    {
        var game = await _gameStore.Retrieve(gameId);

        // we'll backfill this later with real services, but here you go
        if (!game.RequireSynchronizedStart)
            return game.IsLive ? GameStartPhase.Started : GameStartPhase.GameOver;

        // for external/sync start, currently
        var gameModeStartService = ResolveGameModeStartService(game);
        return await gameModeStartService.GetStartPhase(gameId);
    }

    public async Task HandleSyncStartStateChanged(string gameId)
    {
        var state = await _syncStartGameService.GetSyncStartState(gameId);
        await _gameHubBus.SendSyncStartGameStateChanged(state);

        // IFF everyone is ready, start all sessions and return info about them
        if (!state.IsReady)
            return;

        // for now, we're assuming the "happy path" of sync start games being external games, but we'll separate them later
        // var session = await StartSynchronizedSession(gameId); ;
        await Start(new GameStartRequest { GameId = state.Game.Id });
    }

    private IGameModeStartService ResolveGameModeStartService(Data.Game game)
    {
        // three cases to be accommodated: standard challenges, sync start + external (unity), and
        // non-sync-start + external (cubespace)
        // if (game.Mode == GameMode.Standard && !game.RequireSynchronizedStart)
        // {
        //     if (actingUser == null)
        //         throw new CantStartStandardGameWithoutActingUserParameter(gameId);

        //     await _mediator.Send(new StartStandardNonSyncGameCommand(gameId, actingUser));
        // }

        if (game.Mode == GameMode.External && game.RequireSynchronizedStart)
            return _externalSyncGameStartService;

        return null;
    }

    // we need to build the base of the game start data both in the case of doing the actual launch
    // or on demand when someone asks for it, so consolidate that here
    private async Task<GameModeStartRequest> LoadGameModeStartRequest(Data.Game game)
    {
        var now = _now.Get();

        var state = new GameStartState
        {
            Game = new SimpleEntity { Id = game.Id, Name = game.Name },
            Now = now,
            StartTime = now,
        };

        var specs = await _challengeSpecStore.ListAsNoTracking().Where(cs => cs.GameId == game.Id).ToArrayAsync();
        var players = await _playerStore
            .ListAsNoTracking()
            .Where(p => p.GameId == game.Id)
            .ToArrayAsync();
        var teamCaptains = players
            .GroupBy(p => p.TeamId)
            .ToDictionary
            (
                g => g.Key,
                g => _teamService.ResolveCaptain
                (
                    g.Key,
                    _mapper.Map<IEnumerable<Api.Player>>(g.ToList())
                )
            );

        // update state object
        Log($"Data gathered: {players.Length} players on {teamCaptains.Keys.Count}.", game.Id);

        state.ChallengesTotal = specs.Length * teamCaptains.Count;
        state.GamespacesTotal = specs.Length * teamCaptains.Count;
        state.Players.AddRange(players.Select(p => new GameStartStatePlayer
        {
            Player = new SimpleEntity { Id = p.Id, Name = p.ApprovedName },
            TeamId = p.TeamId
        }));

        Log("Identifying team captains...", game.Id);
        // validate that we have a team captain for every team before we do anything
        foreach (var teamId in teamCaptains.Keys)
        {
            if (teamCaptains[teamId] == null)
                throw new CaptainResolutionFailure(teamId, "Couldn't resolve captain during external sync game start.");
        }

        state.Teams.AddRange(teamCaptains.Select(tc => new GameStartStateTeam
        {
            Team = new SimpleEntity { Id = tc.Key, Name = tc.Value.ApprovedName },
            Captain = new GameStartStateTeamCaptain
            {
                Player = new SimpleEntity { Id = tc.Value.Id, Name = tc.Value.ApprovedName },
                UserId = tc.Value.UserId
            },
            HeadlessUrl = null
        }).ToArray());

        return new GameModeStartRequest
        {
            GameId = game.Id,
            State = state,
            Context = new GameModeStartRequestContext
            {
                SessionLengthMinutes = game.SessionMinutes,
                SpecIds = specs.Select(s => s.Id).ToArray(),
            }
        };
    }

    private Task TryCleanupFailedDeploy(GameStartState ctx)
    {
        // TODO
        return Task.CompletedTask;
    }

    private void Log(string message, string gameId)
    {
        var prefix = $"""[START GAME "{gameId}"] - {_now.Get()} - """;
        _logger.LogInformation(message: $"{prefix} {message}");
    }
}
