using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Games.External;
using Gameboard.Api.Features.Teams;
using Gameboard.Api.Structure;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Gameboard.Api.Features.Games.Start;

public interface IGameStartService
{
    Task<GameStartPhase> GetGameStartPhase(string gameId, CancellationToken cancellationToken);
    Task HandleSyncStartStateChanged(string gameId, CancellationToken cancellationToken);
    Task<GameStartState> Start(GameStartRequest request, CancellationToken cancellationToken);
}

internal class GameStartService : IGameStartService
{
    private readonly IExternalSyncGameStartService _externalSyncGameStartService;
    private readonly IGameHubBus _gameHubBus;
    private readonly ILogger<GameStartService> _logger;
    private readonly IMapper _mapper;
    private readonly IMediator _mediator;
    private readonly INowService _now;
    private readonly IStore _store;
    private readonly ISyncStartGameService _syncStartGameService;
    private readonly ITeamService _teamService;

    public GameStartService
    (
        IExternalSyncGameStartService externalSyncGameStartService,
        IGameHubBus gameHubBus,
        ILogger<GameStartService> logger,
        IMediator mediator,
        IMapper mapper,
        INowService now,
        IStore store,
        ISyncStartGameService syncGameStartService,
        ITeamService teamService
    )
    {
        _externalSyncGameStartService = externalSyncGameStartService;
        _gameHubBus = gameHubBus;
        _logger = logger;
        _mapper = mapper;
        _mediator = mediator;
        _now = now;
        _store = store;
        _syncStartGameService = syncGameStartService;
        _teamService = teamService;
    }

    public async Task<GameStartState> Start(GameStartRequest request, CancellationToken cancellationToken)
    {
        var game = await _store.Retrieve<Data.Game>(request.GameId);
        var gameModeStartService = ResolveGameModeStartService(game) ?? throw new NotImplementedException();
        var startRequest = await LoadGameModeStartRequest(game, cancellationToken);

        try
        {
            return await gameModeStartService.Start(startRequest, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(LogEventId.GameStart_Failed, exception: ex, message: $"""Deploy for game "{game.Id}" """);
            await TryCleanupFailedDeploy(startRequest.State);
        }

        return null;
    }

    public async Task<GameStartPhase> GetGameStartPhase(string gameId, CancellationToken cancellationToken)
    {
        var game = await _store.Retrieve<Data.Game>(gameId);

        // we'll backfill this later with real services, but here you go
        if (!game.RequireSynchronizedStart)
            return game.IsLive ? GameStartPhase.Started : GameStartPhase.GameOver;

        // for external/sync start, currently
        var gameModeStartService = ResolveGameModeStartService(game);
        return await gameModeStartService.GetStartPhase(gameId, cancellationToken);
    }

    public async Task HandleSyncStartStateChanged(string gameId, CancellationToken cancellationToken)
    {
        var state = await _syncStartGameService.GetSyncStartState(gameId);
        await _gameHubBus.SendSyncStartGameStateChanged(state);

        // IFF everyone is ready, start all sessions and return info about them
        if (!state.IsReady)
            return;

        // for now, we're assuming the "happy path" of sync start games being external games, but we'll separate them later
        // var session = await StartSynchronizedSession(gameId); ;
        await Start(new GameStartRequest { GameId = state.Game.Id }, cancellationToken);
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

        throw new NotImplementedException();
    }

    // load the data used for game start
    private async Task<GameModeStartRequest> LoadGameModeStartRequest(Data.Game game, CancellationToken cancellationToken)
    {
        var now = _now.Get();

        var state = new GameStartState
        {
            Game = new SimpleEntity { Id = game.Id, Name = game.Name },
            Now = now,
            StartTime = now,
        };

        var players = await _store
            .ListAsNoTracking<Data.Player>()
            .Where(p => p.GameId == game.Id)
            .ToArrayAsync(cancellationToken);

        if (!players.Any())
            throw new CantStartGameWithNoPlayers(game.Id);

        var specs = await _store
            .ListAsNoTracking<Data.ChallengeSpec>()
            .Where(cs => cs.GameId == game.Id)
            .Where(cs => !cs.Disabled)
            .ToArrayAsync(cancellationToken);

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

        // validate that we have a team captain for every team before we do anything
        Log("Identifying team captains...", game.Id);
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

    private async Task TryCleanupFailedDeploy(GameStartState ctx)
    {
        _logger.LogError(message: $"Deployment failed for game {ctx.Game.Id}. Resetting sessions and cleaning up gamespaces for {ctx.Teams.Count} teams.");

        foreach (var team in ctx.Teams)
        {
            try
            {
                await ResetSession(ctx, team.Team.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(message: $"Cleanup failed for team {team.Team.Id}", exception: ex);
            }
        }
    }

    private async Task ResetSession(GameStartState ctx, string teamId)
    {
        await _mediator.Send(new ResetSessionCommand(teamId, false));
    }

    private void Log(string message, string gameId)
    {
        var prefix = $"""[START GAME "{gameId}"] - {_now.Get()} - """;
        _logger.LogInformation(message: $"{prefix} {message}");
    }
}
