using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Data;
using Gameboard.Api.Data.Abstractions;
using Gameboard.Api.Features.Challenges;
using Gameboard.Api.Features.GameEngine;
using Gameboard.Api.Features.Games.Start;
using Gameboard.Api.Features.Teams;
using Gameboard.Api.Services;
using Gameboard.Api.Structure.MediatR;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Gameboard.Api.Features.Games.External;

public interface IExternalSyncGameStartService : IGameModeStartService { }

internal class ExternalSyncGameStartService : IExternalSyncGameStartService
{
    private readonly ChallengeService _challengeService;
    private readonly IChallengeStore _challengeStore;
    private readonly IStore<Data.ChallengeSpec> _challengeSpecStore;
    private readonly IExternalGameDeployBatchService _externalGameDeployBatchService;
    private readonly IExternalGameTeamService _externalGameTeamService;
    private readonly IGamebrainService _gamebrainService;
    private readonly IGameEngineService _gameEngineService;
    private readonly IGameHubBus _gameHubBus;
    private readonly IChallengeGraderUrlService _graderUrlService;
    private readonly IJsonService _jsonService;
    private readonly ILockService _lockService;
    private readonly ILogger<ExternalSyncGameStartService> _logger;
    private readonly IMapper _mapper;
    private readonly IMediator _mediator;
    private readonly IPlayerStore _playerStore;
    private readonly INowService _now;
    private readonly IStore _store;
    private readonly ISyncStartGameService _syncStartGameService;
    private readonly ITeamService _teamService;
    private readonly IValidatorService<GameModeStartRequest> _validator;

    public ExternalSyncGameStartService
    (
        ChallengeService challengeService,
        IChallengeStore challengeStore,
        IStore<Data.ChallengeSpec> challengeSpecStore,
        IExternalGameDeployBatchService externalGameDeployBatchService,
        IExternalGameTeamService externalGameTeamService,
        IGamebrainService gamebrainService,
        IGameEngineService gameEngineService,
        IGameHubBus gameHubBus,
        IChallengeGraderUrlService graderUrlService,
        IJsonService jsonService,
        ILockService lockService,
        ILogger<ExternalSyncGameStartService> logger,
        IMapper mapper,
        IMediator mediator,
        INowService now,
        IPlayerStore playerStore,
        IStore store,
        ISyncStartGameService syncStartGameService,
        ITeamService teamService,
        IValidatorService<GameModeStartRequest> validator
    )
    {
        _challengeService = challengeService;
        _challengeStore = challengeStore;
        _challengeSpecStore = challengeSpecStore;
        _externalGameDeployBatchService = externalGameDeployBatchService;
        _externalGameTeamService = externalGameTeamService;
        _gamebrainService = gamebrainService;
        _gameEngineService = gameEngineService;
        _gameHubBus = gameHubBus;
        _graderUrlService = graderUrlService;
        _jsonService = jsonService;
        _lockService = lockService;
        _logger = logger;
        _mapper = mapper;
        _mediator = mediator;
        _now = now;
        _playerStore = playerStore;
        _store = store;
        _syncStartGameService = syncStartGameService;
        _teamService = teamService;
        _validator = validator;
    }

    public async Task ValidateStart(GameModeStartRequest request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Validating external / sync-start game request...", request.GameId);

        _validator.AddValidator(async (req, ctx) =>
        {
            // just do exists here since we need the game for other checks anyway
            var game = await _store.FirstOrDefaultAsync<Data.Game>(g => g.Id == req.GameId, cancellationToken);

            if (game == null)
            {
                ctx.AddValidationException(new ResourceNotFound<Data.Game>(req.GameId));
                return;
            }

            if (!game.RequireSynchronizedStart)
                ctx.AddValidationException(new GameIsNotSyncStart(game.Id, $"""{nameof(ExternalSyncGameStartService)} can't start this game because it's not sync-start."""));

            if (game.Mode != GameEngineMode.External)
                ctx.AddValidationException(new GameModeIsntExternal(game.Id, $"""{nameof(ExternalSyncGameStartService)} can't start this game because it's not an external game."""));
        });

        _validator.AddValidator(async (req, ctx) =>
        {
            var syncStartState = await _syncStartGameService.GetSyncStartState(req.GameId, cancellationToken);

            if (!syncStartState.IsReady)
                ctx.AddValidationException(new CantStartNonReadySynchronizedGame(syncStartState));
        });

        await _validator.Validate(request, cancellationToken);
        _logger.LogInformation("Validation complete.", request.GameId);
    }

    public async Task<GameStartState> Start(GameModeStartRequest request, CancellationToken cancellationToken)
    {
        // for each team, create metadata that ties them to this game, holds team-specific metadata,
        // and knows about the deploy state
        Log("Creating teams for the external game...", request.GameId);
        await _externalGameTeamService.CreateTeams(request.GameId, request.State.Teams.Select(t => t.Team.Id), cancellationToken);

        Log("Gathering data...", request.GameId);
        await _gameHubBus.SendExternalGameLaunchStart(request.State);

        // update the external team metadata to reflect that we're deploying
        await _externalGameTeamService.UpdateGameDeployStatus(request.GameId, ExternalGameDeployStatus.Deploying, cancellationToken);

        // throw on cancel request so we can clean up the debris
        cancellationToken.ThrowIfCancellationRequested();

        // note that we don't start the transaction until after we've updated the deploy status.
        // we do this because we want to be sure the deploy status shows that we're deploying
        // while the work is happening.
        await _store.DoTransaction(async dbContext =>
        {
            // deploy challenges and gamespaces
            var challengeDeployResults = await DeployChallenges(request, cancellationToken);
            // var challengeGamespaces = await DeployGamespaces(request, cancellationToken);
            // SOON
            var challengeGamespaces = await DeployGamespacesAsync(request, cancellationToken);

            // establish all sessions
            _logger.LogInformation("Starting a synchronized session for all teams...", request.GameId);
            var syncGameStartState = await _syncStartGameService.StartSynchronizedSession(request.GameId, 15, cancellationToken);
            _logger.LogInformation("Synchronized session started!", request.GameId);

            // notify gameboard to move players along
            await _gameHubBus.SendSyncStartGameStarting(syncGameStartState);

            // update external host and get configuration information for teams
            var externalHostTeamConfigs = await NotifyExternalGameHost(request, syncGameStartState, cancellationToken);
            // then assign a headless server to each team
            foreach (var team in request.State.Teams)
            {
                var config = externalHostTeamConfigs.SingleOrDefault(t => t.TeamID == team.Team.Id);
                if (config is null)
                    _logger.LogError($"""Team "{team.Team.Id}" wasn't assigned a headless URL by the external host (Gamebrain).""");
                else
                {
                    // update the request state thing with the team's headless url
                    team.HeadlessUrl = config.HeadlessServerUrl;
                    // but also record it in the DB in case someone cache clears or rejoins from a different machine/browser
                    await _externalGameTeamService.UpdateTeamExternalUrl(team.Team.Id, config.HeadlessServerUrl, cancellationToken);
                }
            }

            // last, update the team/game external deploy status to show we're done
            await _externalGameTeamService.UpdateGameDeployStatus(request.GameId, ExternalGameDeployStatus.Deployed, cancellationToken);
        }, cancellationToken);

        // on we go
        await _gameHubBus.SendExternalGameLaunchEnd(request.State);

        return request.State;
    }

    public async Task<GameStartPhase> GetStartPhase(string gameId, string teamId, CancellationToken cancellationToken)
    {
        // the GameStartService, which calls this code, already checks for game start/end dates,
        // so all we have to do is load team data and translate "external game deploy states" to
        // more general GameStartPhase
        var teamExternalData = await _externalGameTeamService.GetTeam(teamId, cancellationToken);
        if (teamExternalData is null)
            return GameStartPhase.NotStarted;

        return teamExternalData.DeployStatus switch
        {
            ExternalGameDeployStatus.NotStarted => GameStartPhase.NotStarted,
            ExternalGameDeployStatus.Deploying => GameStartPhase.Starting,
            ExternalGameDeployStatus.Deployed => GameStartPhase.Started,
            _ => GameStartPhase.Failed,
        };
    }

    public async Task TryCleanUpFailedDeploy(GameModeStartRequest request, Exception exception)
    {
        // log the error
        var exceptionMessage = $"""EXTERNAL GAME LAUNCH FAILURE (game "{request.GameId}"): {exception.GetType().Name} :: {exception.Message}""";
        _logger.LogError(message: exceptionMessage);
        request.State.Error = exceptionMessage;

        // notify the teams that something is amiss
        await _gameHubBus.SendExternalGameLaunchFailure(request.State);

        // the challenges don't get created upon failure here (thanks to a db transaction)
        // but we still need to clean up any created tm gamespaces
        foreach (var gamespace in request.State.GamespacesStarted)
        {
            try
            {
                var gamespaceChallenge = request.State.ChallengesCreated.SingleOrDefault(c => c.Challenge.Id == gamespace.Id);
                if (gamespaceChallenge is null)
                {
                    Log($"Couldn't completing gamespace with id {gamespace.Id} - couldn't locate a matching deployed challenge.", request.GameId);
                    continue;
                }

                await _gameEngineService.CompleteGamespace(gamespaceChallenge.Challenge.Id, gamespaceChallenge.GameEngineType);
            }
            catch (Exception topoGamespaceDeleteEx)
            {
                Log($"Error completing gamespace with id {request.GameId}: {topoGamespaceDeleteEx.GetType().Name} :: {topoGamespaceDeleteEx.Message} ", request.GameId);
            }
        }

        // note that the GameStartService automatically resets player sessions without unenrolling them after this function is called
    }

    private async Task<IDictionary<string, List<Challenge>>> DeployChallenges(GameModeStartRequest request, CancellationToken cancellationToken)
    {
        Log($"Deploying {request.State.ChallengesTotal} challenges/gamespaces...", request.GameId);
        var teamDeployedChallenges = new Dictionary<string, List<Challenge>>();

        // deploy all challenges
        await _gameHubBus.SendExternalGameChallengesDeployStart(request.State);

        foreach (var team in request.State.Teams)
        {
            // hold onto each team's challenges
            Log($"""Deploying challenges for team "{team.Team.Id}".""", request.GameId);
            teamDeployedChallenges.Add(team.Team.Id, new List<Challenge>());

            foreach (var specId in request.Context.SpecIds)
            {
                Log($"""Creating challenge for spec "{specId}"...""", request.GameId);

                var challenge = await _challengeService.Create
                (
                    new NewChallenge
                    {
                        PlayerId = team.Captain.Player.Id,
                        SpecId = specId,
                        StartGamespace = false, // hold gamespace startup until we're sure we've created all challenges
                        Variant = 0
                    },
                    team.Captain.UserId,
                    _graderUrlService.BuildGraderUrl(),
                    cancellationToken
                );

                request.State.ChallengesCreated.Add(_mapper.Map<GameStartStateChallenge>(challenge));
                teamDeployedChallenges[team.Team.Id].Add(challenge);
                Log($"Spec instantiated for team {team.Team.Id}.", request.GameId);

                await _gameHubBus.SendExternalGameChallengesDeployProgressChange(request.State);
            }
        }

        // notify and return
        await _gameHubBus.SendExternalGameChallengesDeployEnd(request.State);
        return teamDeployedChallenges;
    }

    private async Task<IDictionary<string, ExternalGameStartTeamGamespace>> DeployGamespacesAsync(GameModeStartRequest request, CancellationToken cancellationToken)
    {
        // Startup for gamespace deploy
        Log("Deploying gamespaces...", request.GameId);
        await _gameHubBus.SendExternalGameGamespacesDeployStart(request.State);
        var challengeGamespaces = new Dictionary<string, ExternalGameStartTeamGamespace>();

        // Create one task for each gamespace in batches of the size specified in the app's
        // helm chart config
        var gamespaceDeployBatches = _externalGameDeployBatchService.BuildDeployBatches(request);
        var challengeStates = new Dictionary<string, GameEngineGameState>();

        Log($"Using {gamespaceDeployBatches.Count()} batches to deploy {request.State.ChallengesTotal} gamespaces...", request.GameId);

        // managing an index for debugging - we'll likely pull this before launch.
        var batchIndex = 0;
        foreach (var batch in gamespaceDeployBatches.ToArray())
        {
            var deployResults = await Task.WhenAll(batch.ToArray());

            foreach (var deployResult in deployResults)
                challengeStates.Add(deployResult.Id, deployResult);

            Log($"Finish gamespace batch {++batchIndex}.", request.GameId);
        }

        foreach (var deployedChallenge in request.State.ChallengesCreated)
        {
            // TODO: verify that we need this - buildmetadata also assembles challenge info
            var state = challengeStates[deployedChallenge.Challenge.Id];
            var vms = _gameEngineService.GetGamespaceVms(state);
            challengeGamespaces.Add(deployedChallenge.Challenge.Id, new ExternalGameStartTeamGamespace
            {
                Id = state.Id,
                VmUris = vms.Select(vm => vm.Url)
            });

            // now that we've started the gamespaces, we need to update the challenge entities
            // with the VMs that topo has (hopefully) spun up
            var serializedState = _jsonService.Serialize(state);
            await _store
                .WithNoTracking<Data.Challenge>()
                .Where(c => c.Id == deployedChallenge.Challenge.Id)
                .ExecuteUpdateAsync(up => up.SetProperty(c => c.State, serializedState), cancellationToken);

            await _gameHubBus.SendExternalGameGamespacesDeployProgressChange(request.State);
        }

        // notify and return
        await _gameHubBus.SendExternalGameGamespacesDeployEnd(request.State);
        return challengeGamespaces;
    }

    private async Task<IDictionary<string, ExternalGameStartTeamGamespace>> DeployGamespaces(GameModeStartRequest request, CancellationToken cancellationToken)
    {
        // start all gamespaces
        Log("Deploying gamespaces...", request.GameId);
        var challengeGamespaces = new Dictionary<string, ExternalGameStartTeamGamespace>();

        await _gameHubBus.SendExternalGameGamespacesDeployStart(request.State);

        foreach (var deployedChallenge in request.State.ChallengesCreated)
        {
            _logger.LogInformation(message: $"""Starting {deployedChallenge.GameEngineType} gamespace for challenge "{deployedChallenge.Challenge.Id}" (teamId "{deployedChallenge.TeamId}")...""");
            var challengeState = await _gameEngineService.StartGamespace(new GameEngineGamespaceStartRequest
            {
                ChallengeId = deployedChallenge.Challenge.Id,
                GameEngineType = deployedChallenge.GameEngineType
            });
            _logger.LogInformation(message: $"""Gamespace started for challenge "{deployedChallenge.Challenge.Id}".""");

            // TODO: verify that we need this - buildmetadata also assembles challenge info
            var vms = _gameEngineService.GetGamespaceVms(challengeState);
            challengeGamespaces.Add(deployedChallenge.Challenge.Id, new ExternalGameStartTeamGamespace
            {
                Id = challengeState.Id,
                VmUris = vms.Select(vm => vm.Url)
            });

            // now that we've started the gamespaces, we need to update the challenge entities
            // with the VMs that topo has (hopefully) spun up
            var serializedState = _jsonService.Serialize(challengeState);
            await _store
                .WithNoTracking<Data.Challenge>()
                .Where(c => c.Id == deployedChallenge.Challenge.Id)
                .ExecuteUpdateAsync(up => up.SetProperty(c => c.State, serializedState), cancellationToken);

            request.State.GamespacesStarted.Add(challengeState);
            await _gameHubBus.SendExternalGameGamespacesDeployProgressChange(request.State);
        }

        // notify and return
        await _gameHubBus.SendExternalGameGamespacesDeployEnd(request.State);
        return challengeGamespaces;
    }

    private ExternalGameStartMetaData BuildExternalGameMetaData(GameStartState startState, SyncStartGameStartedState syncgameStartState)
    {
        // build team objects to return
        var teamsToReturn = new List<ExternalGameStartMetaDataTeam>();
        foreach (var team in startState.Teams)
        {
            var teamChallenges = startState.ChallengesCreated.Where(c => c.TeamId == team.Team.Id).Select(c => c.Challenge);
            var teamGameStates = startState.GamespacesStarted.Where(g => teamChallenges.Select(c => c.Id).Contains(g.Id));

            var teamToReturn = new ExternalGameStartMetaDataTeam
            {
                Id = team.Team.Id,
                Name = team.Team.Name,
                Gamespaces = teamGameStates.Select(gs => new ExternalGameStartTeamGamespace
                {
                    Id = gs.Id,
                    VmUris = _gameEngineService.GetGamespaceVms(gs).Select(vm => vm.Url)
                })
            };

            teamsToReturn.Add(teamToReturn);
        }

        var retVal = new ExternalGameStartMetaData
        {
            Game = startState.Game,
            Session = new ExternalGameStartMetaDataSession
            {
                Now = _now.Get(),
                SessionBegin = syncgameStartState.SessionBegin,
                SessionEnd = syncgameStartState.SessionEnd
            },
            Teams = teamsToReturn
        };

        var metadataJson = _jsonService.Serialize(retVal);
        _logger.LogInformation(message: $"""Final metadata payload for game "{retVal.Game.Id}" is here: {metadataJson}.""");
        return retVal;
    }

    private async Task<IEnumerable<ExternalGameClientTeamConfig>> NotifyExternalGameHost(GameModeStartRequest request, SyncStartGameStartedState syncGameStartState, CancellationToken cancellationToken)
    {
        // NOTIFY EXTERNAL CLIENT
        _logger.LogInformation("Notifying external game host (Gamebrain)...");
        // build metadata for external host
        var metaData = BuildExternalGameMetaData(request.State, syncGameStartState);
        var externalClientTeamConfigs = await _gamebrainService.StartGame(metaData);
        _logger.LogInformation("Gamebrain notified!");

        return externalClientTeamConfigs;
    }

    private void Log(string message, string gameId)
    {
        var prefix = $"""[EXTERNAL / SYNC - START GAME "{gameId}"] - {_now.Get()} - """;
        _logger.LogInformation(message: $"{prefix} {message}");
    }
}
