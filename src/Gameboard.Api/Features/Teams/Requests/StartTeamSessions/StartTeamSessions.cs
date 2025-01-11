using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Games;
using Gameboard.Api.Features.Games.External;
using Gameboard.Api.Features.Users;
using Gameboard.Api.Structure;
using Gameboard.Api.Structure.MediatR;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ServiceStack;

namespace Gameboard.Api.Features.Teams;

public record StartTeamSessionsCommand(IEnumerable<string> TeamIds) : IRequest<StartTeamSessionsResult>;

internal sealed class StartTeamSessionsHandler
(
    IActingUserService actingUserService,
    IExternalGameHostService externalGameHostService,
    IGameModeServiceFactory gameModeServiceFactory,
    IGameResourcesDeployService gameResourcesDeploymentService,
    IInternalHubBus internalHubBus,
    ILockService lockService,
    ILogger<StartTeamSessionsHandler> logger,
    IMediator mediator,
    INowService now,
    ISessionWindowCalculator sessionWindowCalculator,
    IStore store,
    ITeamService teamService,
    IUserRolePermissionsService permissionsService,
    IGameboardRequestValidator<StartTeamSessionsCommand> validator
) : IRequestHandler<StartTeamSessionsCommand, StartTeamSessionsResult>
{
    private readonly User _actingUser = actingUserService.Get();
    private readonly IExternalGameHostService _externalGameHostService = externalGameHostService;
    private readonly IGameModeServiceFactory _gameModeServiceFactory = gameModeServiceFactory;
    private readonly IGameResourcesDeployService _gameResourcesDeployService = gameResourcesDeploymentService;
    private readonly IInternalHubBus _internalHubBus = internalHubBus;
    private readonly ILockService _lockService = lockService;
    private readonly ILogger<StartTeamSessionsHandler> _logger = logger;
    private readonly IMediator _mediator = mediator;
    private readonly INowService _now = now;
    private readonly IUserRolePermissionsService _permissionsService = permissionsService;
    private readonly ISessionWindowCalculator _sessionWindow = sessionWindowCalculator;
    private readonly IStore _store = store;
    private readonly ITeamService _teamService = teamService;
    private readonly IGameboardRequestValidator<StartTeamSessionsCommand> _validator = validator;

    public async Task<StartTeamSessionsResult> Handle(StartTeamSessionsCommand request, CancellationToken cancellationToken)
    {
        await _validator.Validate(request, cancellationToken);

        // throw on cancel request so we can clean up the debris
        cancellationToken.ThrowIfCancellationRequested();

        _logger.LogInformation($"Gathering data for game start (teams: {request.TeamIds.ToDelimited()})");
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
            .ToLookupAsync(p => p.TeamId, cancellationToken);

        var gameId = teams.SelectMany(kv => kv.Value.Select(p => p.GameId)).Distinct().Single();
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
                await _permissionsService.Can(PermissionKey.Play_IgnoreExecutionWindow),
                _now.Get()
            );

            // start all sessions
            // (note that this effectively synchronizes all teams starting in this command)
            _logger.LogInformation($"Starting sessions for {request.TeamIds.Count()} teams...");
            await _teamService.SetSessionWindow(request.TeamIds, sessionWindow, cancellationToken);
            _logger.LogInformation($"Sessions started.");

            // alert external host if needed
            if (gameData.Mode == GameEngineMode.External)
                await _externalGameHostService.StartGame(request.TeamIds, sessionWindow, cancellationToken);

            var dict = new Dictionary<string, StartTeamSessionsResultTeam>();
            var finalTeams = teams.Select(kv =>
            {
                var captain = kv.Value.OrderByDescending(p => p.IsManager).First();

                return new StartTeamSessionsResultTeam
                {
                    Id = kv.Key,
                    Name = captain.ApprovedName,
                    ResourcesDeploying = gameData.Mode == GameEngineMode.External,
                    Captain = captain.ToSimpleEntity(p => p.Id, p => p.ApprovedName),
                    Players = kv.Value.Select(p => new SimpleEntity
                    {
                        Id = p.Id,
                        Name = p.ApprovedName
                    }),
                    SessionWindow = sessionWindow
                };
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
            await _mediator.Publish(new GameLaunchFailureNotification(gameId, request.TeamIds, ex.Message), cancellationToken);

            // allow the start service to do custom cleanup
            await modeService.TryCleanUpFailedDeploy(startRequest, ex, cancellationToken);

            // for convenience, reset (but don't unenroll) the teams
            _logger.LogError(message: $"Deployment failed for game {startRequest.Game.Id} ({ex.Message}). Resetting sessions for {startRequest.TeamIds.Count()} teams.");

            foreach (var teamId in request.TeamIds)
            {
                // only archive challenges if the game mode asks us to
                await _mediator.Send(new ResetTeamSessionCommand(teamId, modeService.StartFailResetType, _actingUser), cancellationToken);
            }

            _logger.LogInformation($"All teams reset for game {startRequest.Game.Id}.");

            throw;
        }
    }
}
