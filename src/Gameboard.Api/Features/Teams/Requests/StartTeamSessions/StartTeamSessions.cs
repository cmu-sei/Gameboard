using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Games;
using Gameboard.Api.Features.Games.External;
using Gameboard.Api.Services;
using Gameboard.Api.Structure;
using Gameboard.Api.Structure.MediatR;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Gameboard.Api.Features.Teams;

public record StartTeamSessionsCommand(IEnumerable<string> TeamIds) : IRequest<StartTeamSessionsResult>;

internal sealed class StartTeamSessionsHandler : IRequestHandler<StartTeamSessionsCommand, StartTeamSessionsResult>
{
    private readonly User _actingUser;
    private readonly IExternalGameHostService _externalGameHostService;
    private readonly IGameHubService _gameHubService;
    private readonly IGameModeServiceFactory _gameModeServiceFactory;
    private readonly IGameResourcesDeployService _gameResourcesDeployService;
    private readonly IGameService _gameService;
    private readonly IInternalHubBus _internalHubBus;
    private readonly ILockService _lockService;
    private readonly ILogger<StartTeamSessionsHandler> _logger;
    private readonly IMediator _mediator;
    private readonly INowService _now;
    private readonly ISessionWindowCalculator _sessionWindow;
    private readonly IStore _store;
    private readonly ITeamService _teamService;
    private readonly IGameboardRequestValidator<StartTeamSessionsCommand> _validator;

    public StartTeamSessionsHandler
    (
        IActingUserService actingUserService,
        IExternalGameHostService externalGameHostService,
        IGameHubService gameHubService,
        IGameModeServiceFactory gameModeServiceFactory,
        IGameResourcesDeployService gameResourcesDeploymentService,
        IGameService gameService,
        IInternalHubBus internalHubBus,
        ILockService lockService,
        ILogger<StartTeamSessionsHandler> logger,
        IMediator mediator,
        INowService now,
        ISessionWindowCalculator sessionWindowCalculator,
        IStore store,
        ITeamService teamService,
        IGameboardRequestValidator<StartTeamSessionsCommand> validator
    )
    {
        _actingUser = actingUserService.Get();
        _externalGameHostService = externalGameHostService;
        _gameHubService = gameHubService;
        _gameModeServiceFactory = gameModeServiceFactory;
        _gameResourcesDeployService = gameResourcesDeploymentService;
        _mediator = mediator;
        _now = now;
        _gameService = gameService;
        _lockService = lockService;
        _logger = logger;
        _internalHubBus = internalHubBus;
        _sessionWindow = sessionWindowCalculator;
        _store = store;
        _teamService = teamService;
        _validator = validator;
    }

    public async Task<StartTeamSessionsResult> Handle(StartTeamSessionsCommand request, CancellationToken cancellationToken)
    {
        await _validator.Validate(request, cancellationToken);

        // throw on cancel request so we can clean up the debris
        cancellationToken.ThrowIfCancellationRequested();

        _logger.LogInformation($"Gathering data for game start (teams: {request.TeamIds.ToDelimited()})", "resolving game...");
        var teams = await _store
            .WithNoTracking<Data.Player>()
            .Where(p => request.TeamIds.Contains(p.TeamId))
            .Select(p => new
            {
                p.Id,
                p.ApprovedName,
                p.IsManager,
                p.UserId,
                p.TeamId,
                p.GameId
            })
            .GroupBy(p => p.TeamId)
            .ToDictionaryAsync(gr => gr.Key, gr => gr.ToArray(), cancellationToken);

        var gameId = teams.SelectMany(kv => kv.Value.Select(p => p.GameId)).Single();
        var gameData = await _store
            .WithNoTracking<Data.Game>()
            .Where(g => g.Id == gameId)
            .Select(g => new
            {
                g.Name,
                g.Mode,
                g.SessionMinutes,
                g.GameEnd
            })
            .SingleAsync(cancellationToken);

        // different game modes have different needs on session start - get a service for this game's mode
        var modeService = await _gameModeServiceFactory.Get(gameId);

        // lock it down
        using var gameLock = await _lockService.GetStartSessionLock(gameId).LockAsync(cancellationToken);

        try
        {
            // update hub listeners
            var gameHubEvent = new GameHubEvent { GameId = gameId, TeamIds = request.TeamIds };
            await _mediator.Publish(new GameLaunchStartedNotification(gameId, request.TeamIds), cancellationToken);

            if (modeService.DeployResourcesOnSessionStart)
            {
                foreach (var teamId in request.TeamIds)
                    await _gameResourcesDeployService.DeployResources(teamId, cancellationToken);
            }

            var sessionWindow = _sessionWindow.Calculate
            (
                gameData.SessionMinutes,
                gameData.GameEnd,
                _gameService.IsGameStartSuperUser(_actingUser),
                _now.Get()
            );

            // start all sessions
            await _store
                .WithNoTracking<Data.Player>()
                .Where(p => request.TeamIds.Contains(p.TeamId))
                .ExecuteUpdateAsync
                (
                    up => up
                        .SetProperty(p => p.IsLateStart, sessionWindow.IsLateStart)
                        .SetProperty(p => p.SessionMinutes, sessionWindow.LengthInMinutes)
                        .SetProperty(p => p.SessionBegin, sessionWindow.Start)
                        .SetProperty(p => p.SessionEnd, sessionWindow.End),
                    cancellationToken
                );

            if (modeService.RequireSynchronizedSessions)
            {
                _logger.LogInformation($"Synchronizing gamespaces for {request.TeamIds.Count()} teams...");
                foreach (var teamId in request.TeamIds)
                    await _teamService.UpdateSessionStartAndEnd(teamId, sessionWindow.Start, sessionWindow.End, cancellationToken);

                _logger.LogInformation($"Sessions synchronized.");
            }

            // alert external host if needed
            if (gameData.Mode == GameEngineMode.External)
            {
                await _externalGameHostService.StartGame(request.TeamIds, sessionWindow, cancellationToken);
            }

            var dict = new Dictionary<string, StartTeamSessionsResultTeam>();
            var finalTeams = teams.Select(kv => new StartTeamSessionsResultTeam
            {
                Id = kv.Key,
                Name = kv.Value.Single(p => p.IsManager).ApprovedName,
                ResourcesDeploying = gameData.Mode == GameEngineMode.External,
                Captain = kv.Value.Single(p => p.IsManager).ToSimpleEntity(p => p.Id, p => p.ApprovedName),
                Players = kv.Value.Select(p => new SimpleEntity
                {
                    Id = p.Id,
                    Name = p.ApprovedName
                }),
                SessionWindow = sessionWindow
            }).ToArray();

            foreach (var team in finalTeams)
            {
                dict.Add(team.Id, team);
                await _internalHubBus.SendTeamSessionStarted(team, gameId, _actingUser);
            }

            await _mediator.Publish(new GameLaunchEndedNotification(gameId, request.TeamIds), cancellationToken);
            return new StartTeamSessionsResult
            {
                SessionWindow = sessionWindow,
                Teams = dict
            };
        }
        catch (Exception ex)
        {
            var startRequest = new GameModeStartRequest { Game = new SimpleEntity { Id = gameId, Name = gameData.Name }, TeamIds = request.TeamIds };
            _logger.LogError(LogEventId.GameStart_Failed, exception: ex, message: ex.Message);
            await _mediator.Publish(new GameResourcesDeployFailedNotification(gameId, request.TeamIds, ex.Message), cancellationToken);

            // allow the start service to do custom cleanup
            await modeService.TryCleanUpFailedDeploy(startRequest, ex, cancellationToken);

            // for convenience, reset (but don't unenroll) the teams
            _logger.LogError(message: $"Deployment failed for game {startRequest.Game.Id}. Resetting sessions for {startRequest.TeamIds.Count()} teams.");

            foreach (var teamId in request.TeamIds)
            {
                // only archive challenges if the game mode asks us to
                await _mediator.Send(new ResetTeamSessionCommand(teamId, modeService.StartFailResetType, _actingUser), cancellationToken);
            }

            _logger.LogInformation($"All teams reset for game {startRequest.Game.Id}.");
        }

        return null;
    }
}
