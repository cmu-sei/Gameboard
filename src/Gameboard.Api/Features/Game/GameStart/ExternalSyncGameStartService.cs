using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Data;
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
    private readonly INowService _now;
    private readonly IStore _store;
    private readonly ISyncStartGameService _syncStartGameService;
    private readonly ITeamService _teamService;
    private readonly IValidatorService<GameModeStartRequest> _validator;

    public ExternalSyncGameStartService
    (
        ChallengeService challengeService,
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
        IStore store,
        ISyncStartGameService syncStartGameService,
        ITeamService teamService,
        IValidatorService<GameModeStartRequest> validator
    )
    {
        _challengeService = challengeService;
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
        _store = store;
        _syncStartGameService = syncStartGameService;
        _teamService = teamService;
        _validator = validator;
    }

    public async Task ValidateStart(GameModeStartRequest request, CancellationToken cancellationToken)
    {
        Log("Validating external / sync-start game request...", request.Game.Id);

        _validator.AddValidator(async (req, ctx) =>
        {
            // just do exists here since we need the game for other checks anyway
            var game = await _store.FirstOrDefaultAsync<Data.Game>(g => g.Id == req.Game.Id, cancellationToken);

            if (game == null)
            {
                ctx.AddValidationException(new ResourceNotFound<Data.Game>(req.Game.Id));
                return;
            }

            if (!game.RequireSynchronizedStart)
                ctx.AddValidationException(new GameIsNotSyncStart(game.Id, $"""{nameof(ExternalSyncGameStartService)} can't start this game because it's not sync-start."""));

            if (game.Mode != GameEngineMode.External)
                ctx.AddValidationException(new GameModeIsntExternal(game.Id, $"""{nameof(ExternalSyncGameStartService)} can't start this game because it's not an external game."""));
        });

        _validator.AddValidator(async (req, ctx) =>
        {
            var syncStartState = await _syncStartGameService.GetSyncStartState(req.Game.Id, cancellationToken);

            if (!syncStartState.IsReady)
                ctx.AddValidationException(new CantStartNonReadySynchronizedGame(syncStartState));
        });

        await _validator.Validate(request, cancellationToken);
        Log($"Validation complete.", request.Game.Id);
    }

    public async Task<GameStartContext> Start(GameModeStartRequest request, CancellationToken cancellationToken)
    {
        // for each team, create metadata that ties them to this game, holds team-specific metadata,
        // and knows about the deploy state
        Log("Creating teams for the external game...", request.Game.Id);
        await _externalGameTeamService.CreateTeams(request.Game.Id, request.Context.Teams.Select(t => t.Team.Id), cancellationToken);

        Log("Gathering data...", request.Game.Id);
        await _gameHubBus.SendExternalGameLaunchStart(request.Context.ToUpdate());

        // update the external team metadata to reflect that we're deploying
        await _externalGameTeamService.UpdateGameDeployStatus(request.Game.Id, ExternalGameDeployStatus.Deploying, cancellationToken);

        // throw on cancel request so we can clean up the debris
        cancellationToken.ThrowIfCancellationRequested();

        // note that we don't start the transaction until after we've updated the deploy status.
        // we do this because we want to be sure the deploy status shows that we're deploying
        // while the work is happening.
        await _store.DoTransaction(async dbContext =>
        {
            // deploy challenges and gamespaces
            var deployedResources = await DeployResources(request, cancellationToken);

            // establish all sessions
            Log("Starting a synchronized session for all teams...", request.Game.Id);
            var syncGameStartState = await _syncStartGameService.StartSynchronizedSession(request.Game.Id, 15, cancellationToken);
            Log("Synchronized session started!", request.Game.Id);

            // notify gameboard to move players along
            await _gameHubBus.SendSyncStartGameStarting(syncGameStartState);

            // update external host and get configuration information for teams
            var externalHostTeamConfigs = await NotifyExternalGameHost(request, syncGameStartState, cancellationToken);
            // then assign a headless server to each team
            foreach (var team in request.Context.Teams)
            {
                var config = externalHostTeamConfigs.SingleOrDefault(t => t.TeamID == team.Team.Id);
                if (config is null)
                    Log($"Team {team.Team.Id} wasn't assigned a headless URL by the external host (Gamebrain).", request.Game.Id);
                else
                {
                    // update the request state thing with the team's headless url
                    team.HeadlessUrl = config.HeadlessServerUrl;
                    // but also record it in the DB in case someone cache clears or rejoins from a different machine/browser
                    await _externalGameTeamService.UpdateTeamExternalUrl(team.Team.Id, config.HeadlessServerUrl, cancellationToken);
                }
            }

            // last, update the team/game external deploy status to show we're done
            await _externalGameTeamService.UpdateGameDeployStatus(request.Game.Id, ExternalGameDeployStatus.Deployed, cancellationToken);
        }, cancellationToken);

        // on we go
        await _gameHubBus.SendExternalGameLaunchEnd(request.Context.ToUpdate());

        return request.Context;
    }

    public async Task<GameStartDeployedResources> DeployResources(GameModeStartRequest request, CancellationToken cancellationToken)
    {
        // deploy challenges and gamespaces
        var challengeDeployResults = await DeployChallenges(request, cancellationToken);
        // var gamespaces = await DeployGamespaces(request, cancellationToken);
        // SOON
        var gamespaces = await DeployGamespacesAsync(request, cancellationToken);

        var teamIds = challengeDeployResults
            .Select(c => c.Key)
            .ToArray();

        // if any gamespaces were created, this deploy is invalid and we bail
        var challengeIdsWithNoGamespace = challengeDeployResults
            .SelectMany(c => c.Value)
            .Where(c => !gamespaces.ContainsKey(c.Id))
            .Select(c => c.Id)
            .ToArray();

        if (challengeIdsWithNoGamespace.Any())
            throw new InvalidOperationException($"Challenges were deployed without a corresponding gamespace: {string.Join(",", challengeIdsWithNoGamespace)}");

        // compose return value from deployed resources
        var retVal = new Dictionary<string, GameStartDeployedTeamResources>();
        foreach (var teamId in teamIds)
        {
            var deployedResources = challengeDeployResults[teamId].Select(challenge =>
            {
                return new GameStartDeployedChallenge
                {
                    Challenge = new SimpleEntity { Id = challenge.Id, Name = challenge.Name },
                    GameEngineType = challenge.GameEngineType,
                    Gamespace = gamespaces[challenge.Id],
                    TeamId = teamId
                };
            });

            retVal.Add(teamId, new GameStartDeployedTeamResources { Resources = deployedResources });
        }

        return new GameStartDeployedResources
        {
            Game = request.Game,
            DeployedResources = retVal
        };
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
        var exceptionMessage = $"""EXTERNAL GAME LAUNCH FAILURE (game "{request.Game.Id}"): {exception.GetType().Name} :: {exception.Message}""";
        Log(exceptionMessage, request.Game.Id);
        request.Context.Error = exceptionMessage;

        // notify the teams that something is amiss
        await _gameHubBus.SendExternalGameLaunchFailure(request.Context.ToUpdate());

        // the challenges don't get created upon failure here (thanks to a db transaction)
        // but we still need to clean up any created tm gamespaces
        foreach (var gamespace in request.Context.GamespacesStarted)
        {
            try
            {
                var gamespaceChallenge = request.Context.ChallengesCreated.SingleOrDefault(c => c.Challenge.Id == gamespace.Id);
                if (gamespaceChallenge is null)
                {
                    Log($"Couldn't completing gamespace with id {gamespace.Id} - couldn't locate a matching deployed challenge.", request.Game.Id);
                    continue;
                }

                await _gameEngineService.CompleteGamespace(gamespaceChallenge.Challenge.Id, gamespaceChallenge.GameEngineType);
            }
            catch (Exception topoGamespaceDeleteEx)
            {
                Log($"Error completing gamespace with id {request.Game.Id}: {topoGamespaceDeleteEx.GetType().Name} :: {topoGamespaceDeleteEx.Message} ", request.Game.Id);
            }
        }

        // note that the GameStartService automatically resets player sessions without unenrolling them after this function is called
    }

    /// <summary>
    /// Deploy challenges given a request (containing gameId and specs to deploy).
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns>A dictionary of teamId -> list of challenges</returns>
    private async Task<IDictionary<string, List<Challenge>>> DeployChallenges(GameModeStartRequest request, CancellationToken cancellationToken)
    {
        Log($"Deploying {request.Context.TotalChallengeCount} challenges/gamespaces...", request.Game.Id);
        var teamDeployedChallenges = new Dictionary<string, List<Challenge>>();

        // deploy all challenges
        await _gameHubBus.SendExternalGameChallengesDeployStart(request.Context.ToUpdate());
        var teamIds = request.Context.Teams.Select(t => t.Team.Id).ToArray();

        // determine which, if any, challenges have been predeployed for this game
        var predeployedChallenges = await _store
            .WithNoTracking<Data.Challenge>()
            .Where(c => teamIds.Contains(c.TeamId))
            .GroupBy(c => c.TeamId)
            .ToDictionaryAsync(g => g.Key, g => g.ToArray(), cancellationToken);

        foreach (var team in request.Context.Teams)
        {
            // hold onto each team's challenges
            Log($"""Deploying challenges for team "{team.Team.Id}".""", request.Game.Id);
            teamDeployedChallenges.Add(team.Team.Id, new List<Challenge>());

            // Load predeployed challenges in case we can skip any
            var teamPreDeployedChallenges = predeployedChallenges.ContainsKey(team.Team.Id) ?
                predeployedChallenges[team.Team.Id] : Array.Empty<Data.Challenge>();
            Log($"""Team {team.Team.Id} has {teamPreDeployedChallenges.Length} predeployed challenge(s).""", request.Game.Id);

            foreach (var specId in request.Context.SpecIds)
            {
                // check if we've already deployed this team's copy of the challenge (through predeployment, for example)
                var deployedChallenge = _mapper.Map<Challenge>(teamPreDeployedChallenges.SingleOrDefault(c => c.SpecId == specId));

                if (deployedChallenge is not null)
                {
                    Log($"Team {team.Team.Id} already has a challenge for spec {specId}. Skipping deployment...", request.Game.Id);
                }
                else
                {
                    Log($"""Creating challenge for team {team.Team.Id}, spec "{specId}"...""", request.Game.Id);
                    deployedChallenge = await _challengeService.Create
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
                }

                request.Context.ChallengesCreated.Add(new GameStartContextChallenge
                {
                    Challenge = new SimpleEntity { Id = deployedChallenge.Id, Name = deployedChallenge.Name },
                    GameEngineType = deployedChallenge.GameEngineType,
                    State = deployedChallenge.State,
                    TeamId = team.Team.Id
                });
                teamDeployedChallenges[team.Team.Id].Add(deployedChallenge);
                Log($"Spec instantiated for team {team.Team.Id}.", request.Game.Id);

                await _gameHubBus.SendExternalGameChallengesDeployProgressChange(request.Context.ToUpdate());
            }
        }

        // notify and return
        await _gameHubBus.SendExternalGameChallengesDeployEnd(request.Context.ToUpdate());
        return teamDeployedChallenges;
    }

    private async Task<IDictionary<string, ExternalGameStartTeamGamespace>> DeployGamespaces(GameModeStartRequest request, CancellationToken cancellationToken)
    {
        // start all gamespaces
        Log("Deploying gamespaces...", request.Game.Id);
        var challengeGamespaces = new Dictionary<string, ExternalGameStartTeamGamespace>();
        await _gameHubBus.SendExternalGameGamespacesDeployStart(request.Context.ToUpdate());

        foreach (var deployedChallenge in request.Context.ChallengesCreated)
        {
            var challengeState = deployedChallenge.State;

            if (challengeState.HasDeployedGamespace)
            {
                Log($"{deployedChallenge.GameEngineType} gamespace for challenge {deployedChallenge.Challenge.Id} (teamId {deployedChallenge.TeamId}) is already running. Skipping startup...", request.Game.Id);
            }
            else
            {
                Log($"""Starting {deployedChallenge.GameEngineType} gamespace for challenge "{deployedChallenge.Challenge.Id}" (teamId "{deployedChallenge.TeamId}")...""", request.Game.Id);
                challengeState = await _gameEngineService.StartGamespace(new GameEngineGamespaceStartRequest
                {
                    ChallengeId = deployedChallenge.Challenge.Id,
                    GameEngineType = deployedChallenge.GameEngineType
                });
                Log(message: $"""Gamespace started for challenge "{deployedChallenge.Challenge.Id}".""", request.Game.Id);
            }

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

            request.Context.GamespacesStarted.Add(challengeState);
            await _gameHubBus.SendExternalGameGamespacesDeployProgressChange(request.Context.ToUpdate());
        }

        // notify and return
        await _gameHubBus.SendExternalGameGamespacesDeployEnd(request.Context.ToUpdate());
        return challengeGamespaces;
    }

    private async Task<IDictionary<string, ExternalGameStartTeamGamespace>> DeployGamespacesAsync(GameModeStartRequest request, CancellationToken cancellationToken)
    {
        // Startup for gamespace deploy
        Log("Deploying gamespaces...", request.Game.Id);
        await _gameHubBus.SendExternalGameGamespacesDeployStart(request.Context.ToUpdate());
        var retVal = new Dictionary<string, ExternalGameStartTeamGamespace>();

        // determine which challenges have been predeployed so we can skip them here
        var notPredeployedChallenges = request.Context.ChallengesCreated.Where(c => !c.State.HasDeployedGamespace).ToArray();
        var predeployedChallenges = request.Context.ChallengesCreated.Where(c => c.State.HasDeployedGamespace).ToArray();
        Log($"{notPredeployedChallenges.Length} require deployment ({predeployedChallenges.Length} predeployed)...", request.Game.Id);

        // add all the predeployed gamespaces to our list so that it contains _all_ gamespaces at the end of this function
        foreach (var predeployedState in predeployedChallenges.Select(c => c.State))
        {
            retVal.Add(predeployedState.Id, ChallengeStateToTeamGamespace(predeployedState));
            request.Context.GamespacesStarted.Add(predeployedState);
            await _gameHubBus.SendExternalGameGamespacesDeployProgressChange(request.Context.ToUpdate());
        }

        // Create one task for each gamespace in batches of the size specified in the app's
        // helm chart config
        var batchIndex = 0;
        var challengeBatches = _externalGameDeployBatchService.BuildDeployBatches(notPredeployedChallenges);

        Log($"Using {challengeBatches.Count()} batches to deploy {request.Context.TotalGamespaceCount} gamespaces...", request.Game.Id);

        foreach (var batch in challengeBatches.ToArray())
        {
            Log($"Starting gamespace batch #{++batchIndex} ({batch.ToArray().Length} challenges)...", request.Game.Id);

            // resolve the challenges in this batch to tasks that call the game engine and ask it to start a gamespace
            var batchTasks = batch.Select(async challenge =>
            {
                _logger.LogInformation(message: $"""Starting {challenge.GameEngineType} gamespace for challenge "{challenge.Challenge.Id}" (teamId "{challenge.TeamId}")...""");

                var challengeState = await _gameEngineService.StartGamespace(new GameEngineGamespaceStartRequest
                {
                    ChallengeId = challenge.Challenge.Id,
                    GameEngineType = challenge.GameEngineType
                });
                Log($"""Gamespace started for challenge "{challenge.Challenge.Id}".""", request.Game.Id);

                // now that the gamespace has started, update gameboard's database so it knows
                // about the new state
                var serializedState = _jsonService.Serialize(challengeState);
                await _store
                    .WithNoTracking<Data.Challenge>()
                    .Where(c => c.Id == challenge.Challenge.Id)
                    .ExecuteUpdateAsync(up => up.SetProperty(c => c.State, serializedState));

                // record the created challenge/state
                Log($"""Gamespace saved for challenge "{challenge.Challenge.Id}".""", request.Game.Id);
                request.Context.GamespacesStarted.Add(challengeState);

                // transform info about the VMs so we can return them later, then report progress and move on
                var gamespace = ChallengeStateToTeamGamespace(challengeState);
                retVal.Add(gamespace.Id, gamespace);
                Log($"Challenge {challengeState.Id} has {gamespace.VmUris.Count()} VMs.", request.Game.Id);
                await _gameHubBus.SendExternalGameGamespacesDeployProgressChange(request.Context.ToUpdate());

                // return the challenge state
                return challengeState;
            });

            var deployResults = await Task.WhenAll(batchTasks.ToArray());
            Log($"Finished {deployResults.Length} tasks done for gamespace batch #{batchIndex}.", request.Game.Id);
        }

        // notify and return
        Log($"Finished deploying gamespaces.", request.Game.Id);
        await _gameHubBus.SendExternalGameGamespacesDeployEnd(request.Context.ToUpdate());
        return retVal;
    }

    private ExternalGameStartMetaData BuildExternalGameMetaData(GameStartContext context, SyncStartGameStartedState syncgameStartState)
    {
        // build team objects to return
        var teamsToReturn = new List<ExternalGameStartMetaDataTeam>();
        foreach (var team in context.Teams)
        {
            var teamChallenges = context.ChallengesCreated.Where(c => c.TeamId == team.Team.Id).Select(c => c.Challenge);
            var teamGameStates = context.GamespacesStarted.Where(g => teamChallenges.Select(c => c.Id).Contains(g.Id));

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
            Game = context.Game,
            Session = new ExternalGameStartMetaDataSession
            {
                Now = _now.Get(),
                SessionBegin = syncgameStartState.SessionBegin,
                SessionEnd = syncgameStartState.SessionEnd
            },
            Teams = teamsToReturn
        };

        var metadataJson = _jsonService.Serialize(retVal);
        Log($"""Final metadata payload for game "{retVal.Game.Id}" is here: {metadataJson}.""", retVal.Game.Id);
        return retVal;
    }

    private async Task<IEnumerable<ExternalGameClientTeamConfig>> NotifyExternalGameHost(GameModeStartRequest request, SyncStartGameStartedState syncGameStartState, CancellationToken cancellationToken)
    {
        // NOTIFY EXTERNAL CLIENT
        Log("Notifying external game host (Gamebrain)...", request.Game.Id);
        // build metadata for external host
        var metaData = BuildExternalGameMetaData(request.Context, syncGameStartState);
        var externalClientTeamConfigs = await _gamebrainService.StartGame(metaData);
        Log("External game host notified!", request.Game.Id);

        return externalClientTeamConfigs;
    }

    private ExternalGameStartTeamGamespace ChallengeStateToTeamGamespace(GameEngineGameState state)
    {
        var vms = _gameEngineService.GetGamespaceVms(state).Select(vm => vm.Url);

        return new ExternalGameStartTeamGamespace
        {
            Id = state.Id,
            VmUris = _gameEngineService.GetGamespaceVms(state).Select(vm => vm.Url)
        };
    }

    private void Log(string message, string gameId)
    {
        var prefix = $"""[EXTERNAL / SYNC - START GAME "{gameId}"] - {_now.Get()} - """;
        _logger.LogInformation(message: $"{prefix} {message}");
    }
}
