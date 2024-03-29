using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Teams;
using Gameboard.Api.Structure;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Gameboard.Api.Features.Games.Start;

public interface IGameStartService
{
    Task<GamePlayState> GetGamePlayState(string gameId, CancellationToken cancellationToken);
    Task<GameStartDeployedResources> PreDeployGameResources(PreDeployResourcesRequest request, CancellationToken cancellationToken);
    Task<GameStartContext> Start(GameStartRequest request, CancellationToken cancellationToken);
}

internal class GameStartService : IGameStartService
{
    private readonly IActingUserService _actingUserService;
    private readonly IGameModeServiceFactory _gameModeServiceFactory;
    private readonly ILockService _lockService;
    private readonly ILogger<GameStartService> _logger;
    private readonly IMediator _mediator;
    private readonly INowService _now;
    private readonly IStore _store;
    private readonly ITeamService _teamService;

    public GameStartService
    (
        IActingUserService actingUserService,
        IGameModeServiceFactory gameModeServiceFactory,
        ILockService lockService,
        ILogger<GameStartService> logger,
        IMediator mediator,
        INowService now,
        IStore store,
        ITeamService teamService
    )
    {
        _actingUserService = actingUserService;
        _gameModeServiceFactory = gameModeServiceFactory;
        _lockService = lockService;
        _logger = logger;
        _mediator = mediator;
        _now = now;
        _store = store;
        _teamService = teamService;
    }

    public async Task<GameStartDeployedResources> PreDeployGameResources(PreDeployResourcesRequest request, CancellationToken cancellationToken)
    {
        var game = await _store.Retrieve<Data.Game>(request.GameId);
        var gameModeService = await _gameModeServiceFactory.Get(request.GameId);
        var startRequest = await LoadGameModeStartRequest(game, request.TeamIds, cancellationToken);

        // lock this down - only one start or predeploy per game Id
        using var gameStartLock = await _lockService.GetExternalGameDeployLock(request.GameId).LockAsync(cancellationToken);

        _logger.LogInformation($"Pre-deploying game resources for game {request.GameId}...");
        var deployedResources = await gameModeService.DeployResources(startRequest, cancellationToken);
        _logger.LogInformation($"Game resources predeployed for game {request.GameId}.");

        return deployedResources;
    }

    public async Task<GameStartContext> Start(GameStartRequest request, CancellationToken cancellationToken)
    {
        var game = await _store.Retrieve<Data.Game>(request.GameId);
        var gameModeService = await _gameModeServiceFactory.Get(request.GameId);

        // lock this down - only one start or predeploy per game Id
        using var gameStartLock = await _lockService
            .GetExternalGameDeployLock(request.GameId)
            .LockAsync(cancellationToken);

        var startRequest = await LoadGameModeStartRequest(game, null, cancellationToken);
        await gameModeService.ValidateStart(startRequest, cancellationToken);

        try
        {
            return await gameModeService.Start(startRequest, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(LogEventId.GameStart_Failed, exception: ex, message: $"""Deploy for game "{game.Id}" failed.""");

            // allow the start service to do custom cleanup
            await gameModeService.TryCleanUpFailedDeploy(startRequest, ex, cancellationToken);

            // for convenience, reset (but don't unenroll) the teams
            _logger.LogError(message: $"Deployment failed for game {startRequest.Game.Id}. Resetting sessions for {startRequest.Context.Teams.Count} teams.");

            foreach (var team in startRequest.Context.Teams)
            {
                // only archive challenges if the game mode asks us too
                await _mediator.Send(new ResetTeamSessionCommand(team.Team.Id, false, _actingUserService.Get()), cancellationToken);
            }

            _logger.LogInformation($"All teams reset for game {startRequest.Game.Id}.");
        }

        return null;
    }

    public async Task<GamePlayState> GetGamePlayState(string gameId, CancellationToken cancellationToken)
    {
        var game = await _store.WithNoTracking<Data.Game>().SingleAsync(g => g.Id == gameId, cancellationToken);

        // apply all these rules regardless of mode settings
        var nowish = _now.Get();

        // if now is after the game end or after the team's session end, they're done
        if (nowish > game.GameEnd)
            return GamePlayState.GameOver;

        if (nowish < game.GameStart)
            return GamePlayState.NotStarted;

        // right now, external + sync/start is the only mode that has a dedicated service, so handle
        // that here and then just use some simplistic logic for other modes
        if (game.RequireSynchronizedStart && game.Mode == GameEngineMode.External)
        {
            var gameModeStartService = await _gameModeServiceFactory.Get(gameId);
            return await gameModeStartService.GetGamePlayState(gameId, cancellationToken);
        }

        return GamePlayState.Started;
    }

    /// <summary>
    /// No matter which mode the game is set to, there's baseline information we need to execute the start process, teams/players,
    /// the game's challenges, etc. Load all that here to give context to our start request.
    /// 
    /// Note that if the teamIds parameter is empty or null, data for all teams will be loaded. If it's specified, the results
    /// will be constrained to the teamIds passed. This exists to support predeployment - we wanted the ability to predeploy
    /// one team's resources without necessarily deploying the entire game.
    /// </summary>
    /// <param name="game"></param>
    /// <param name="teamIds"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="CantStartGameWithNoPlayers"></exception>
    /// <exception cref="CaptainResolutionFailure"></exception>
    private async Task<GameModeStartRequest> LoadGameModeStartRequest(Data.Game game, IEnumerable<string> teamIds, CancellationToken cancellationToken)
    {
        var now = _now.Get();
        var loadAllTeams = teamIds is null || !teamIds.Any();

        var players = await _store
            .WithNoTracking<Data.Player>()
            .Where(p => p.GameId == game.Id)
            .Where(p => loadAllTeams || teamIds.Contains(p.TeamId))
            .ToArrayAsync(cancellationToken);

        if (!players.Any())
            throw new CantStartGameWithNoPlayers(game.Id);

        var specs = await _store
            .WithNoTracking<Data.ChallengeSpec>()
            .Where(cs => cs.GameId == game.Id)
            .Where(cs => !cs.Disabled)
            .ToArrayAsync(cancellationToken);

        var teamCaptains = players
            .GroupBy(p => p.TeamId)
            .ToDictionary
            (
                g => g.Key,
                g => _teamService.ResolveCaptain(g.ToList())
            );

        // update context and log stuff
        Log($"Data gathered: {players.Length} players on {teamCaptains.Keys.Count} teams.", game.Id);

        var context = new GameStartContext
        {
            Game = new SimpleEntity { Id = game.Id, Name = game.Name },
            StartTime = now,
            SessionLengthMinutes = game.SessionMinutes,
            SpecIds = specs.Select(s => s.Id).ToArray(),
            TotalChallengeCount = specs.Length * teamCaptains.Count,
            TotalGamespaceCount = specs.Length * teamCaptains.Count
        };

        context.Players.AddRange(players.Select(p => new GameStartContextPlayer
        {
            Player = new SimpleEntity { Id = p.Id, Name = p.ApprovedName },
            TeamId = p.TeamId
        }));

        // validate that we have a team captain for every team before we do anything
        Log("Identifying team captains...", game.Id);
        foreach (var teamId in teamCaptains.Keys)
        {
            if (teamCaptains[teamId] == null)
                throw new CaptainResolutionFailure(teamId, "Couldn't resolve team captain during game start.");
        }

        context.Teams.AddRange(teamCaptains.Select(tc => new GameStartContextTeam
        {
            Team = new SimpleEntity { Id = tc.Key, Name = tc.Value.ApprovedName },
            Captain = new GameStartContextTeamCaptain
            {
                Player = new SimpleEntity { Id = tc.Value.Id, Name = tc.Value.ApprovedName },
                UserId = tc.Value.UserId
            },
            HeadlessUrl = null
        }).ToArray());

        return new GameModeStartRequest
        {
            Game = new SimpleEntity { Id = game.Id, Name = game.Name },
            Context = context
        };
    }

    private void Log(string message, string gameId)
    {
        var prefix = $"""[START GAME "{gameId}"] - {_now.Get()} - """;
        _logger.LogInformation(message: $"{prefix} {message}");
    }
}
