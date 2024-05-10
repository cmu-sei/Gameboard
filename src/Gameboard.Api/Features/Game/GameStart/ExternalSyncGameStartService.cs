using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Data;
using Gameboard.Api.Features.GameEngine;
using Gameboard.Api.Features.Games.Start;
using Gameboard.Api.Features.Teams;
using Gameboard.Api.Structure.MediatR;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Gameboard.Api.Features.Games.External;

public interface IExternalSyncGameStartService : IGameModeStartService { }

internal class ExternalSyncGameStartService : IExternalSyncGameStartService
{
    private readonly IExternalGameService _externalGameService;
    private readonly IExternalGameHostService _externalGameHostService;
    private readonly IGameEngineService _gameEngineService;
    private readonly IGameHubService _gameHubBus;
    private readonly IGameResourcesDeploymentService _gameResourcesDeployment;
    private readonly IJsonService _jsonService;
    private readonly ILogger<ExternalSyncGameStartService> _logger;
    private readonly IMapper _mapper;
    private readonly IMediator _mediator;
    private readonly INowService _now;
    private readonly IStore _store;
    private readonly ISyncStartGameService _syncStartGameService;
    private readonly ITeamService _teamService;
    private readonly IValidatorService<GameModeStartRequest> _validator;

    public ExternalSyncGameStartService
    (
        IExternalGameService externalGameService,
        IExternalGameHostService externalGameHostService,
        IGameEngineService gameEngineService,
        IGameHubService gameHubBus,
        IGameResourcesDeploymentService gameResourcesDeployment,
        IJsonService jsonService,
        ILogger<ExternalSyncGameStartService> logger,
        IMapper mapper,
        IMediator mediator,
        INowService now,
        IStore store,
        ISyncStartGameService syncStartGameService,
        ITeamService teamService,
        IValidatorService<GameModeStartRequest> validator
    )
    {
        _externalGameService = externalGameService;
        _externalGameHostService = externalGameHostService;
        _gameEngineService = gameEngineService;
        _gameHubBus = gameHubBus;
        _gameResourcesDeployment = gameResourcesDeployment;
        _jsonService = jsonService;
        _logger = logger;
        _mapper = mapper;
        _mediator = mediator;
        _now = now;
        _store = store;
        _syncStartGameService = syncStartGameService;
        _teamService = teamService;
        _validator = validator;
    }

    public TeamSessionResetType StartFailResetType => TeamSessionResetType.PreserveChallenges;

    public async Task ValidateStart(GameModeStartRequest request, CancellationToken cancellationToken)
    {
        Log("Validating external / sync-start game request...", request.Game.Id);

        _validator.AddValidator(async (req, ctx) =>
        {
            // just do exists here since we need the game for other checks anyway
            var game = await _store
                .WithNoTracking<Data.Game>()
                .Where(g => g.Id == req.Game.Id)
                .Select(g => new
                {
                    g.Id,
                    g.RequireSynchronizedStart,
                    g.Mode
                })
                .SingleOrDefaultAsync(cancellationToken);

            if (game == null)
            {
                ctx.AddValidationException(new ResourceNotFound<Data.Game>(req.Game.Id));
                return;
            }

            if (!game.RequireSynchronizedStart)
                ctx.AddValidationException(new ExternalGameIsNotSyncStart(game.Id, $"""{nameof(ExternalSyncGameStartService)} can't start this game because it's not sync-start."""));

            if (game.Mode != GameEngineMode.External)
                ctx.AddValidationException(new GameModeIsntExternal(game.Id, $"""{nameof(ExternalSyncGameStartService)} can't start this game because it's not an external game."""));
        });

        _validator.AddValidator(async (req, ctx) =>
        {
            var syncStartState = await _syncStartGameService.GetSyncStartState(req.Game.Id, cancellationToken);

            if (!syncStartState.IsReady)
                ctx.AddValidationException(new CantStartNonReadySynchronizedGame(syncStartState));
        });

        _validator.AddValidator(async (req, ctx) =>
        {
            var gamePlayState = await GetGamePlayState(req.Game.Id, cancellationToken);

            if (gamePlayState == GamePlayState.GameOver)
                ctx.AddValidationException(new CantStartGameInIneligiblePlayState(req.Game.Id, gamePlayState));
        });

        await _validator.Validate(request, cancellationToken);
        Log($"Validation complete.", request.Game.Id);
    }

    public async Task<GameStartContext> Start(GameModeStartRequest request, CancellationToken cancellationToken)
    {
        Log($"Launching game {request.Game.Id} with {request.Context.Teams.Count} teams...", request.Game.Id);
        await _gameHubBus.SendExternalGameLaunchStart(request.Context.ToUpdate());

        // deploy challenges and gamespaces
        var teamIds = request.Context.Teams.Select(t => t.Team.Id).ToArray();
        Log($"Deploying resources for {teamIds.Length} team(s)...", request.Game.Id);
        var deployResult = await _gameResourcesDeployment.DeployResources(teamIds, cancellationToken);
        Log("Game resources deployed.", request.Game.Id);

        // establish all sessions
        Log("Starting a synchronized session for all teams...", request.Game.Id);
        var syncGameStartState = await _syncStartGameService.StartSynchronizedSession(request.Game.Id, request.SessionWindow, cancellationToken);
        Log("Synchronized session started!", request.Game.Id);

        // update external host and get configuration information for teams
        await _externalGameService.Start(teamIds, request.SessionWindow, cancellationToken);

        // on we go
        Log("External game launched.", request.Game.Id);
        await _gameHubBus.SendExternalGameLaunchEnd(request.Context.ToUpdate());

        return request.Context;
    }

    public Task<GamePlayState> GetGamePlayState(string gameId, CancellationToken cancellationToken)
        => GetGamePlayStateForGameAndTeam(gameId, null, cancellationToken);

    public Task<GamePlayState> GetGamePlayStateForTeam(string teamId, CancellationToken cancellationToken)
        => GetGamePlayStateForGameAndTeam(null, teamId, cancellationToken);

    private async Task<GamePlayState> GetGamePlayStateForGameAndTeam(string gameId, string teamId, CancellationToken cancellationToken)
    {
        if (teamId.IsNotEmpty())
            gameId = await _teamService.GetGameId(teamId, cancellationToken);

        var gameState = await _externalGameService.GetExternalGameState(gameId, cancellationToken);

        if (gameState.OverallDeployStatus == ExternalGameDeployStatus.NotStarted)
            return GamePlayState.NotStarted;

        if (!gameState.Teams.Any() || gameState.Teams.Any(t => !t.IsReady) || !gameState.Teams.SelectMany(t => t.Challenges).Any())
            return GamePlayState.NotStarted;

        if (gameState.OverallDeployStatus == ExternalGameDeployStatus.Deploying)
            return GamePlayState.DeployingResources;

        if (gameState.OverallDeployStatus == ExternalGameDeployStatus.Deployed)
            return GamePlayState.Started;

        if (gameState.HasNonStandardSessionWindow || gameState.OverallDeployStatus == ExternalGameDeployStatus.PartiallyDeployed)
            return GamePlayState.Starting;

        throw new CantResolveGamePlayState(null, gameId);
    }

    public async Task TryCleanUpFailedDeploy(GameModeStartRequest request, Exception exception, CancellationToken cancellationToken)
    {
        // log the error
        var exceptionMessage = $"""EXTERNAL GAME LAUNCH FAILURE (game "{request.Game.Id}"): {exception.GetType().Name} :: {exception.Message}""";
        Log(exceptionMessage, request.Game.Id);
        request.Context.Error = exceptionMessage;

        // notify the teams that something is amiss
        await _gameHubBus.SendExternalGameLaunchFailure(request.Context.ToUpdate());

        // NOT CLEANING UP GAMESPACES FOR NOW - MAYBE WE CAN REUSE
        // clean up external team metadata (deploy statuses and external links like Unity headless URLs)
        // var cleanupTeamIds = request.Context.Teams.Select(t => t.Team.Id).ToArray();
        // await _externalGameService.DeleteTeamExternalData(cancellationToken, cleanupTeamIds);

        // the GameStartService which orchestrates all game starts will automatically clean up challenges
        // if an exception was thrown, but we still need to clean up any created tm gamespaces (if
        // we're not trying to reuse them)
        // 
        // if (request.AbortOnGamespaceStartFailure)
        // {
        //     foreach (var gamespace in request.Context.GamespacesStarted)
        //     {
        //         try
        //         {
        //             var gamespaceChallenge = request.Context.ChallengesCreated.SingleOrDefault(c => c.Challenge.Id == gamespace.Id);
        //             if (gamespaceChallenge is null)
        //             {
        //                 Log($"Couldn't completing gamespace with id {gamespace.Id} - couldn't locate a matching deployed challenge.", request.Game.Id);
        //                 continue;
        //             }

        //             await _gameEngineService.CompleteGamespace(gamespaceChallenge.Challenge.Id, gamespaceChallenge.GameEngineType);
        //         }
        //         catch (Exception topoGamespaceDeleteEx)
        //         {
        //             Log($"Error completing gamespace with id {request.Game.Id}: {topoGamespaceDeleteEx.GetType().Name} :: {topoGamespaceDeleteEx.Message} ", request.Game.Id);
        //         }
        //     }
        // }

        // note that the GameStartService automatically resets player sessions without unenrolling them after this function is called
    }

    // private ExternalGameStartMetaData BuildExternalGameMetaData(GameStartContext context, SyncStartGameStartedState syncGameStartState)
    // {
    //     // build team objects to return
    //     var teamsToReturn = new List<ExternalGameStartMetaDataTeam>();
    //     foreach (var team in context.Teams)
    //     {
    //         var teamChallenges = context.ChallengesCreated.Where(c => c.TeamId == team.Team.Id).Select(c => c.Challenge).ToArray();
    //         var teamGameStates = context.GamespacesStarted.Where(g => teamChallenges.Select(c => c.Id).Contains(g.Id)).ToArray();
    //         var teamPlayers = !syncGameStartState.Teams.ContainsKey(team.Team.Id) ?
    //              Array.Empty<ExternalGameStartMetaDataPlayer>() :
    //             syncGameStartState.Teams[team.Team.Id]
    //                 .Select(p => new ExternalGameStartMetaDataPlayer
    //                 {
    //                     PlayerId = p.Id,
    //                     UserId = p.UserId
    //                 }).ToArray();

    //         var teamToReturn = new ExternalGameStartMetaDataTeam
    //         {
    //             Id = team.Team.Id,
    //             Name = team.Team.Name,
    //             Gamespaces = teamGameStates.Select(gs => new ExternalGameStartTeamGamespace
    //             {
    //                 Id = gs.Id,
    //                 VmUris = _gameEngineService.GetGamespaceVms(gs).Select(vm => vm.Url),
    //                 IsDeployed = gs.HasDeployedGamespace
    //             }),
    //             Players = teamPlayers
    //         };

    //         teamsToReturn.Add(teamToReturn);
    //     }

    //     var retVal = new ExternalGameStartMetaData
    //     {
    //         Game = context.Game,
    //         Session = new ExternalGameStartMetaDataSession
    //         {
    //             Now = _now.Get(),
    //             SessionBegin = syncGameStartState.SessionBegin,
    //             SessionEnd = syncGameStartState.SessionEnd
    //         },
    //         Teams = teamsToReturn
    //     };

    //     var metadataJson = _jsonService.Serialize(retVal);
    //     Log($"""Final metadata payload for game "{retVal.Game.Id}" is here: {metadataJson}.""", retVal.Game.Id);
    //     return retVal;
    // }

    private void Log(string message, string gameId)
    {
        var prefix = $"[EXTERNAL / SYNC-START GAME {gameId}] - ";
        _logger.LogInformation(message: $"{prefix} {message}");
    }
}
