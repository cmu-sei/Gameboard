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
    private readonly IBatchingService _batchService;
    private readonly ChallengeService _challengeService;
    private readonly CoreOptions _coreOptions;
    private readonly IExternalGameTeamService _externalGameTeamService;
    private readonly IGamebrainService _gamebrainService;
    private readonly IGameEngineService _gameEngineService;
    private readonly IGameHubBus _gameHubBus;
    private readonly IChallengeGraderUrlService _graderUrlService;
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
        IBatchingService batchingService,
        ChallengeService challengeService,
        CoreOptions coreOptions,
        IExternalGameTeamService externalGameTeamService,
        IGamebrainService gamebrainService,
        IGameEngineService gameEngineService,
        IGameHubBus gameHubBus,
        IChallengeGraderUrlService graderUrlService,
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
        _batchService = batchingService;
        _challengeService = challengeService;
        _coreOptions = coreOptions;
        _externalGameTeamService = externalGameTeamService;
        _gamebrainService = gamebrainService;
        _gameEngineService = gameEngineService;
        _gameHubBus = gameHubBus;
        _graderUrlService = graderUrlService;
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
        // for each team, create metadata that ties them to this game, holds team-specific metadata,
        // and knows about the deploy state
        Log("Creating teams for the external game...", request.Game.Id);
        await _externalGameTeamService.CreateTeams(request.Game.Id, request.Context.Teams.Select(t => t.Team.Id), cancellationToken);
        await _gameHubBus.SendExternalGameLaunchStart(request.Context.ToUpdate());

        Log("Gathering deploy data...", request.Game.Id);

        // update the external team metadata to reflect that we're deploying
        await _externalGameTeamService.UpdateGameDeployStatus(request.Game.Id, ExternalGameTeamDeployStatus.Deploying, cancellationToken);

        // throw on cancel request so we can clean up the debris
        cancellationToken.ThrowIfCancellationRequested();

        // deploy challenges and gamespaces
        Log("Deploying game resources...", request.Game.Id);
        var deployedResources = await DeployResources(request, cancellationToken);
        Log("Game resources deployed.", request.Game.Id);

        // we try to preserve as much of the deployment as we can when it fails. as a result,
        // it's possible for DeployResources above to finish without correctly spinning up
        // all gamespaces. check that we have them all before continuing
        var nonStartedGamespaces = deployedResources
            .DeployedResources
            .Select(entry => entry.Value)
            .SelectMany(r => r.Challenges)
            .Where(c => !c.Gamespace.IsDeployed)
            .ToArray();

        if (nonStartedGamespaces.Any())
        {
            var ids = nonStartedGamespaces.Select(c => c.Challenge.Id).ToArray();
            Log($"Can't start game {request.Game.Id} because after resource deploy, {nonStartedGamespaces.Count()} gamespace(s) weren't on: {string.Join(',', ids)}", request.Game.Id);
        }

        // establish all sessions
        Log("Starting a synchronized session for all teams...", request.Game.Id);
        var syncGameStartState = await _syncStartGameService.StartSynchronizedSession(request.Game.Id, 15, cancellationToken);
        Log("Synchronized session started!", request.Game.Id);

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
        await _externalGameTeamService.UpdateGameDeployStatus(request.Game.Id, ExternalGameTeamDeployStatus.Deployed, cancellationToken);

        // on we go
        Log("External game launched.", request.Game.Id);
        await _gameHubBus.SendExternalGameLaunchEnd(request.Context.ToUpdate());

        return request.Context;
    }

    public async Task<GameStartDeployedResources> DeployResources(GameModeStartRequest request, CancellationToken cancellationToken)
    {
        // deploy challenges and gamespaces
        var challengeDeployResults = await DeployChallenges(request, cancellationToken);
        var gamespaces = await DeployGamespaces(request, cancellationToken);

        var teamIds = challengeDeployResults
            .Select(c => c.Key)
            .ToArray();

        // most of the time, a challenge needs an associated gamespace, but apparently in at least one case, it doesn't, so just warn about gamespaceless challenges
        var challengeIdsWithNoGamespace = challengeDeployResults
            .SelectMany(c => c.Value)
            .Where(c => !gamespaces.ContainsKey(c.Id))
            .Select(c => c.Id)
            .ToArray();

        if (challengeIdsWithNoGamespace.Any())
        {
            Log($"WARNING: Some deployed challenges have no gamespaces: {string.Join(",", challengeIdsWithNoGamespace)}", request.Game.Id);
        }

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

            retVal.Add(teamId, new GameStartDeployedTeamResources { Challenges = deployedResources });
        }

        return new GameStartDeployedResources
        {
            Game = request.Game,
            DeployedResources = retVal
        };
    }

    public async Task<GamePlayState> GetGamePlayState(string gameId, CancellationToken cancellationToken)
    {
        // We define an external/sync-start game to be ready if all registered players are sync'd and all teams have 
        // all challenges with deployed gamespaces.

        // we split this into a bunch of smaller queries because EF's assumed behavior was too slow on a real db.
        var players = await _store
            .WithNoTracking<Data.Player>()
            .Where(p => p.GameId == gameId)
            .Select(p => new
            {
                p.Id,
                p.IsReady,
                p.TeamId
            })
            .ToArrayAsync(cancellationToken);

        var challenges = await _store
            .WithNoTracking<Data.Challenge>()
            .Where(c => c.GameId == gameId)
            .Select(c => new
            {
                c.Id,
                c.HasDeployedGamespace,
                c.TeamId,
                c.SpecId,
                c.Score,
                c.Points
            })
            .ToArrayAsync(cancellationToken);

        // if any players aren't ready, we haven't started yet
        if (players.Any(p => !p.IsReady))
            return GamePlayState.NotStarted;

        // have to load specs separately because ugh
        var specIds = await _store
            .WithNoTracking<Data.ChallengeSpec>()
            .Where(s => s.GameId == gameId)
            .Select(s => s.Id)
            .ToArrayAsync(cancellationToken);

        var teams = players
            .GroupBy(p => p.TeamId)
            .ToDictionary(g => g.Key, g => g.ToArray());

        var teamChallenges = challenges
            .GroupBy(c => c.TeamId)
            .ToDictionary(c => c.Key, c => c.ToArray());

        // ugly iteration time.
        var anyChallenges = teamChallenges.Any(c => true);
        var allDeployed = true;

        if (!anyChallenges)
            return GamePlayState.NotStarted;

        // the yuck part is that we have to determine whether every
        // combination of spec and team exists and is deployed.
        foreach (var specId in specIds)
        {
            foreach (var teamIdIterated in teams.Keys)
            {
                // each team needs either a deployed challenge or an undeployed one that they've completed (based on score)
                var challenge = teamChallenges.ContainsKey(teamIdIterated) ? teamChallenges[teamIdIterated].FirstOrDefault(c => c.SpecId == specId) : null;

                if (challenge is null)
                {
                    _logger.LogInformation($"Game {gameId} is not in Started state because we couldn't find a challenge for team {teamIdIterated} / spec {specId}.");
                    allDeployed = false;
                    break;
                }

                if (!challenge.HasDeployedGamespace && challenge.Score < challenge.Points)
                {
                    _logger.LogInformation($"Game {gameId} is not in Started state because the challenge for team {teamIdIterated} / spec {specId} is undeployed and not finished.");
                    allDeployed = false;
                    break;
                }
            }
        }

        if (allDeployed)
            return GamePlayState.Started;

        return GamePlayState.Starting;
    }

    public async Task TryCleanUpFailedDeploy(GameModeStartRequest request, Exception exception, CancellationToken cancellationToken)
    {
        // log the error
        var exceptionMessage = $"""EXTERNAL GAME LAUNCH FAILURE (game "{request.Game.Id}"): {exception.GetType().Name} :: {exception.Message}""";
        Log(exceptionMessage, request.Game.Id);
        request.Context.Error = exceptionMessage;

        // notify the teams that something is amiss
        await _gameHubBus.SendExternalGameLaunchFailure(request.Context.ToUpdate());

        // the GameStartService which orchestrates all game starts will automatically clean up challenges
        // if an exception was thrown, but we still need to clean up any created tm gamespaces
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

        // also need to clean up external team metadata (deploy statuses and external links like Unity headless URLs)
        var cleanupTeamIds = request.Context.Teams.Select(t => t.Team.Id).ToArray();
        try
        {
            await _externalGameTeamService.DeleteTeamExternalData(cancellationToken, cleanupTeamIds);
        }
        catch (Exception deleteExternalTeamDataException)
        {
            Log($"Error cleaning up external team data (teams: {string.Join(',', cleanupTeamIds)}): {deleteExternalTeamDataException.GetType().Name} :: {deleteExternalTeamDataException.Message}", request.Game.Id);
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
        // Startup for gamespace deploy
        Log("Deploying gamespaces...", request.Game.Id);
        await _gameHubBus.SendExternalGameGamespacesDeployStart(request.Context.ToUpdate());
        var retVal = new Dictionary<string, ExternalGameStartTeamGamespace>();

        // determine which challenges have been predeployed so we can skip them here
        var notPredeployedChallenges = request.Context.ChallengesCreated.Where(c => !c.State.IsActive).ToArray();
        var predeployedChallenges = request.Context.ChallengesCreated.Where(c => c.State.IsActive).ToArray();
        Log($"{notPredeployedChallenges.Length} require deployment ({predeployedChallenges.Length} predeployed)...", request.Game.Id);

        // add all the predeployed gamespaces to our list so that it contains _all_ gamespaces at the end of this function
        foreach (var predeployedState in predeployedChallenges.Select(c => c.State))
        {
            retVal.Add(predeployedState.Id, ChallengeStateToTeamGamespace(predeployedState));
            request.Context.GamespacesStarted.Add(predeployedState);
            await _gameHubBus.SendExternalGameGamespacesDeployProgressChange(request.Context.ToUpdate());
        }

        // if we don't have any gamespaces that aren't predeployed, we can take a big fat shortcut
        if (notPredeployedChallenges.Any())
        {
            // Create one task for each gamespace in batches of the size specified in the app's
            // helm chart config
            var challengeBatches = BuildGamespaceBatches(notPredeployedChallenges, _coreOptions);
            var batchIndex = 0;
            var batchCount = challengeBatches.Count();

            Log($"Using {challengeBatches.Count()} batches to deploy {request.Context.TotalGamespaceCount} gamespaces...", request.Game.Id);

            foreach (var batch in challengeBatches.ToArray())
            {
                Log($"Starting gamespace batch #{++batchIndex} of {batchCount} ({batch.ToArray().Length} challenges)...", request.Game.Id);

                // resolve the challenges in this batch to tasks that call the game engine and ask it to start a gamespace
                var batchTasks = batch.Select(async challenge =>
                {
                    Log(message: $"Starting {challenge.GameEngineType} gamespace for challenge {challenge.Challenge.Id} (teamId {challenge.TeamId})...", request.Game.Id);

                    try
                    {
                        var challengeState = await _gameEngineService.StartGamespace(new GameEngineGamespaceStartRequest
                        {
                            ChallengeId = challenge.Challenge.Id,
                            GameEngineType = challenge.GameEngineType
                        });
                        Log($"Gamespace started for challenge {challenge.Challenge.Id}.", request.Game.Id);

                        // record the created challenge/state for reporting
                        request.Context.GamespacesStarted.Add(challengeState);

                        // transform info about the VMs so we can return them later, then report progress and move on
                        var gamespace = ChallengeStateToTeamGamespace(challengeState);
                        retVal.Add(gamespace.Id, gamespace);

                        Log($"Challenge {gamespace.Id} has {gamespace.VmUris.Count()} VM(s): {string.Join(", ", challengeState.Vms.Select(vm => vm.Name))}", request.Game.Id);
                        await _gameHubBus.SendExternalGameGamespacesDeployProgressChange(request.Context.ToUpdate());

                        // return the engine state of the challenge
                        return challengeState;
                    }
                    catch (GamespaceStartFailure ex)
                    {
                        Log($"Gamespace failed to start for challenge {challenge.Challenge.Id} ({ex.Message}).", request.Game.Id);

                        // We continue on if this flag is false (which is when we're pre-deploying as opposed to deploying live).
                        // The hope is that we can manually start any gamespaces which failed to boot,
                        // so we don't want to clean up the working gamespaces/challenges.
                        if (request.AbortOnGamespaceStartFailure)
                            throw;

                        var failedGamespace = new ExternalGameStartTeamGamespace
                        {
                            Id = challenge.Challenge.Id,
                            IsDeployed = false,
                            VmUris = Array.Empty<string>()
                        };

                        retVal.Add(failedGamespace.Id, failedGamespace);
                        request.Context.GamespaceIdsStartFailed.Add(challenge.Challenge.Id);
                    }
                });

                // fire a thread for each task in the batch
                var deployResults = await Task.WhenAll(batchTasks.ToArray());

                // after the asynchronous part is over, we need to do database updates to ensure the DB has the correct 
                // game-engine-supplied state for each challenge
                foreach (var state in deployResults)
                {
                    var serializedState = _jsonService.Serialize(state);
                    var isGamespaceOn = state.IsActive && state.Vms is not null && state.Vms.Any();

                    await _store
                        .WithNoTracking<Data.Challenge>()
                        .Where(c => c.Id == state.Id)
                        .ExecuteUpdateAsync
                        (
                            up => up
                                .SetProperty(c => c.HasDeployedGamespace, isGamespaceOn)
                                .SetProperty(c => c.State, serializedState)
                        );

                    Log($"Updated gamespace states for challenge {state.Id}. Gamespace on?: {isGamespaceOn}", request.Game.Id);
                }

                Log($"Finished {deployResults.Length} tasks done for gamespace batch #{batchIndex}.", request.Game.Id);
            }
        }

        // notify and return
        var totalGamespaceCount = retVal.Count;
        var totalVmCount = retVal.Values.SelectMany(gamespace => gamespace.VmUris).Count();
        var deployedGamespaceCount = retVal.Values.Select(gamespace => gamespace.IsDeployed).Count();

        Log($"Finished deploying gamespaces: {totalGamespaceCount} gamespaces ({deployedGamespaceCount} ready), {totalVmCount} visible VMs.", request.Game.Id);
        await _gameHubBus.SendExternalGameGamespacesDeployEnd(request.Context.ToUpdate());
        return retVal;
    }

    private IEnumerable<IEnumerable<GameStartContextChallenge>> BuildGamespaceBatches(IEnumerable<GameStartContextChallenge> challenges, CoreOptions coreOptions)
    {
        var batchSize = coreOptions.GameEngineDeployBatchSize;
        if (batchSize < 1)
            batchSize = 1;

        return _batchService.Batch(challenges, batchSize);
    }

    private ExternalGameStartMetaData BuildExternalGameMetaData(GameStartContext context, SyncStartGameStartedState syncGameStartState)
    {
        // build team objects to return
        var teamsToReturn = new List<ExternalGameStartMetaDataTeam>();
        foreach (var team in context.Teams)
        {
            var teamChallenges = context.ChallengesCreated.Where(c => c.TeamId == team.Team.Id).Select(c => c.Challenge).ToArray();
            var teamGameStates = context.GamespacesStarted.Where(g => teamChallenges.Select(c => c.Id).Contains(g.Id)).ToArray();
            var teamPlayers = !syncGameStartState.Teams.ContainsKey(team.Team.Id) ?
                 Array.Empty<ExternalGameStartMetaDataPlayer>() :
                syncGameStartState.Teams[team.Team.Id]
                    .Select(p => new ExternalGameStartMetaDataPlayer
                    {
                        PlayerId = p.Id,
                        UserId = p.UserId
                    }).ToArray();

            var teamToReturn = new ExternalGameStartMetaDataTeam
            {
                Id = team.Team.Id,
                Name = team.Team.Name,
                Gamespaces = teamGameStates.Select(gs => new ExternalGameStartTeamGamespace
                {
                    Id = gs.Id,
                    VmUris = _gameEngineService.GetGamespaceVms(gs).Select(vm => vm.Url),
                    IsDeployed = gs.HasDeployedGamespace
                }),
                Players = teamPlayers
            };

            teamsToReturn.Add(teamToReturn);
        }

        var retVal = new ExternalGameStartMetaData
        {
            Game = context.Game,
            Session = new ExternalGameStartMetaDataSession
            {
                Now = _now.Get(),
                SessionBegin = syncGameStartState.SessionBegin,
                SessionEnd = syncGameStartState.SessionEnd
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
        => new()
        {
            Id = state.Id,
            IsDeployed = state.IsActive,
            VmUris = _gameEngineService.GetGamespaceVms(state).Select(vm => vm.Url)
        };

    private void Log(string message, string gameId)
    {
        var prefix = $"[EXTERNAL / SYNC-START GAME {gameId}] - ";
        _logger.LogInformation(message: $"{prefix} {message}");
    }
}
