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
using Gameboard.Api.Features.Teams;
using Gameboard.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Gameboard.Api.Features.Games;

public interface IGameResourcesDeploymentService
{
    public Task<GameResourcesDeployResults> DeployResources(string teamId, CancellationToken cancellationToken);
    public Task<GameResourcesDeployResults> DeployResources(IEnumerable<string> teamIds, CancellationToken cancellationToken);
}

internal class GameResourcesDeploymentService : IGameResourcesDeploymentService
{
    private readonly IBatchingService _batchingService;
    private readonly ChallengeService _challengeService;
    private readonly CoreOptions _coreOptions;
    private readonly IGameEngineService _gameEngineService;
    private readonly IChallengeGraderUrlService _graderUrlService;
    private readonly IJsonService _jsonService;
    private readonly ILockService _lockService;
    private readonly ILogger<GameResourcesDeploymentService> _logger;
    private readonly IMapper _mapper;
    private readonly IStore _store;
    private readonly ITeamService _teamService;

    public GameResourcesDeploymentService
    (
        IBatchingService batchingService,
        ChallengeService challengeService,
        CoreOptions coreOptions,
        IGameEngineService gameEngineService,
        IChallengeGraderUrlService graderUrlService,
        IJsonService jsonService,
        ILockService lockService,
        ILogger<GameResourcesDeploymentService> logger,
        IMapper mapper,
        IStore store,
        ITeamService teamService
    )
    {
        _batchingService = batchingService;
        _challengeService = challengeService;
        _coreOptions = coreOptions;
        _gameEngineService = gameEngineService;
        _graderUrlService = graderUrlService;
        _jsonService = jsonService;
        _lockService = lockService;
        _logger = logger;
        _mapper = mapper;
        _store = store;
        _teamService = teamService;
    }

    public Task<GameResourcesDeployResults> DeployResources(string teamId, CancellationToken cancellationToken)
        => DeployResources(new string[] { teamId }, cancellationToken);

    public async Task<GameResourcesDeployResults> DeployResources(IEnumerable<string> teamIds, CancellationToken cancellationToken)
    {
        var game = await _store
            .WithNoTracking<Data.Player>()
                .Include(p => p.Game)
            .Where(p => teamIds.Contains(p.TeamId))
            .Select(p => new SimpleEntity { Id = p.GameId, Name = p.Game.Name })
            .SingleAsync(cancellationToken);

        var request = new GameResourcesDeployRequest
        {
            GameId = game.Id,
            SpecIds = await ResolveChallengeSpecIds(game.Id, cancellationToken),
            TeamIds = teamIds
        };

        // lock this down - only one start or predeploy per game Id
        using var gameStartLock = await _lockService.GetExternalGameDeployLock(request.GameId).LockAsync(cancellationToken);

        // deploy challenges and gamespaces
        Log($"Deploying {request.SpecIds.Count()} challenges for teams {string.Join(", ", request.TeamIds)}...", request.GameId);
        var challengeDeployResults = await DeployChallenges(request, cancellationToken);
        Log($"Starting {request.SpecIds.Count()} gamespaces for teams {string.Join(", ", request.TeamIds)}...", request.GameId);
        var gamespaceDeployResults = await DeployGamespaces(request.GameId, challengeDeployResults.SelectMany(kv => kv.Value), cancellationToken);

        // most of the time, a challenge needs an associated gamespace, but apparently in at least one case, it doesn't, so just warn about gamespaceless challenges
        var challengeIdsWithNoGamespace = challengeDeployResults
            .SelectMany(c => c.Value)
            .Where(c => !gamespaceDeployResults.Gamespaces.ContainsKey(c.Id))
            .Select(c => c.Id)
            .ToArray();

        if (challengeIdsWithNoGamespace.Any())
            Log($"WARNING: Some deployed challenges have no gamespaces: {string.Join(",", challengeIdsWithNoGamespace)}", request.GameId);

        // log that we're done deploying these teams
        // await _externalGameService.UpdateTeamDeployStatus(teamIds, ExternalGameDeployStatus.Deployed, cancellationToken);

        Log($"{request.SpecIds.Count()} challenges deployed for teams {string.Join(", ", request.TeamIds)}...", request.GameId);

        return new GameResourcesDeployResults
        {
            Game = game,
            TeamChallenges = challengeDeployResults,
            DeployFailedGamespaceIds = gamespaceDeployResults.FailedGamespaceDeployIds
        };
    }

    /// <summary>
    /// Deploy challenges given a request (containing gameId and specs to deploy).
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns>A dictionary of teamId -> list of challenges</returns>
    private async Task<IDictionary<string, IEnumerable<GameResourcesDeployChallenge>>> DeployChallenges(GameResourcesDeployRequest request, CancellationToken cancellationToken)
    {
        var teamDeployedChallenges = new Dictionary<string, List<GameResourcesDeployChallenge>>();
        // await _gameHubBus.SendExternalGameChallengesDeployStart(request.Context.ToUpdate());

        // determine which, if any, challenges have been predeployed for this game
        var predeployedChallenges = await _store
            .WithNoTracking<Data.Challenge>()
            .Where(c => request.TeamIds.Contains(c.TeamId))
            .Where(c => request.SpecIds.Contains(c.SpecId))
            .Where(c => request.GameId == c.GameId)
            .GroupBy(c => c.TeamId)
            .ToDictionaryAsync(g => g.Key, g => g.ToArray(), cancellationToken);

        foreach (var teamId in request.TeamIds)
        {
            var captain = await _teamService.ResolveCaptain(teamId, cancellationToken);

            // hold onto each team's challenges
            Log($"Deploying challenges for team {teamId}...", request.GameId);
            teamDeployedChallenges.Add(teamId, new List<GameResourcesDeployChallenge>());

            // Load predeployed challenges in case we can skip any
            var teamPreDeployedChallenges = predeployedChallenges.ContainsKey(teamId) ?
                predeployedChallenges[teamId] : Array.Empty<Data.Challenge>();
            Log($"Team has {teamPreDeployedChallenges.Length} predeployed challenge(s).", teamId);

            foreach (var specId in request.SpecIds)
            {
                // check if we've already deployed this team's copy of the challenge (through predeployment, for example)
                var deployedChallenge = _mapper.Map<Challenge>(teamPreDeployedChallenges.SingleOrDefault(c => c.SpecId == specId));
                if (deployedChallenge is not null)
                {
                    Log($"Team {teamId} already has a challenge for spec {specId}. Skipping deployment...", request.GameId);
                }
                else
                {
                    Log($"Creating challenge for team {teamId}, spec {specId}...", request.GameId);
                    deployedChallenge = await _challengeService.Create
                    (
                        new NewChallenge
                        {
                            PlayerId = captain.Id,
                            SpecId = specId,
                            StartGamespace = false,
                            Variant = 0
                        },
                        captain.UserId,
                        _graderUrlService.BuildGraderUrl(),
                        cancellationToken
                    );
                }

                // request.Context.ChallengesCreated.Add(new GameStartContextChallenge
                // {
                //     Challenge = new SimpleEntity { Id = deployedChallenge.Id, Name = deployedChallenge.Name },
                //     GameEngineType = deployedChallenge.GameEngineType,
                //     IsFullySolved = deployedChallenge.Score >= deployedChallenge.Points,
                //     State = deployedChallenge.State,
                //     TeamId = teamId
                // });

                teamDeployedChallenges[teamId].Add(new GameResourcesDeployChallenge
                {
                    Id = deployedChallenge.Id,
                    Name = deployedChallenge.Name,
                    Engine = deployedChallenge.GameEngineType,
                    IsActive = deployedChallenge.State.IsActive,
                    IsFullySolved = deployedChallenge.Score >= deployedChallenge.Points,
                    SpecId = deployedChallenge.SpecId,
                    State = deployedChallenge.State,
                    TeamId = deployedChallenge.TeamId
                });

                Log($"Spec {deployedChallenge.SpecId} instantiated for team {teamId}.", request.GameId);
                // await _gameHubBus.SendExternalGameChallengesDeployProgressChange(request.Context.ToUpdate());
            }
        }
        // notify and return
        return teamDeployedChallenges.ToDictionary(kv => kv.Key, kv => kv.Value.AsEnumerable());
    }

    private async Task<GameResourcesDeployGamespacesResult> DeployGamespaces(string gameId, IEnumerable<GameResourcesDeployChallenge> challenges, CancellationToken cancellationToken)
    {
        // Startup for gamespace deploy
        Log("Deploying gamespaces...", gameId);
        // await _gameHubBus.SendExternalGameGamespacesDeployStart(request.Context.ToUpdate());
        var deployedGamespaces = new Dictionary<string, GameResourcesDeployGamespace>();
        var failedDeployGamespaceIds = new List<string>();

        // determine which challenges have been predeployed so we can skip them here
        // note that challenges which have been solved are also skipped (we don't want to redeploy gamespaces for solved challenges if a deploy request occurs
        // after the game has started)
        var notPredeployedChallenges = challenges.Where(c => !c.State.IsActive && !c.IsFullySolved).ToArray();
        var predeployedChallenges = challenges.Where(c => !notPredeployedChallenges.Any(d => d.Id == c.Id)).ToArray();

        Log($"There are {challenges.Count()} total challenges.", gameId);
        Log($"{notPredeployedChallenges.Length} require deployment.", gameId);
        Log($"{challenges.Where(c => c.IsFullySolved).Count()} are fully solved.", gameId);
        Log($"{challenges.Where(c => !c.IsFullySolved && c.State.IsActive).Count()} have been predeployed.", gameId);

        // add all the predeployed gamespaces to our list so that it contains _all_ gamespaces at the end of this function
        foreach (var predeployedState in predeployedChallenges.Select(c => c.State))
        {
            deployedGamespaces.Add(predeployedState.Id, ChallengeStateToTeamGamespace(predeployedState));
            // request.Context.GamespacesStarted.Add(predeployedState);
            // await _gameHubBus.SendExternalGameGamespacesDeployProgressChange(request.Context.ToUpdate());
        }

        // if we don't have any gamespaces that aren't predeployed, we can take a big fat shortcut
        if (notPredeployedChallenges.Any())
        {
            // Create one task for each gamespace in batches of the size specified in the app's
            // helm chart config (min batch size 1)
            var challengeBatches = _batchingService.Batch(notPredeployedChallenges, Math.Max(_coreOptions.GameEngineDeployBatchSize, 1));
            var batchIndex = 0;
            var batchCount = challengeBatches.Count();

            Log($"Using {challengeBatches.Count()} batches to deploy {notPredeployedChallenges.Length} gamespaces...", gameId);

            foreach (var batch in challengeBatches.ToArray())
            {
                Log($"Starting gamespace batch #{++batchIndex} of {batchCount} ({batch.ToArray().Length} challenges)...", gameId);

                // resolve the challenges in this batch to tasks that call the game engine and ask it to start a gamespace
                var batchTasks = batch.Select(async challenge =>
                {
                    Log(message: $"Starting {challenge.Engine} gamespace for challenge {challenge.Id} (teamId {challenge.TeamId})...", gameId);

                    try
                    {
                        var challengeState = await _gameEngineService.StartGamespace(new GameEngineGamespaceStartRequest
                        {
                            ChallengeId = challenge.Id,
                            GameEngineType = challenge.Engine
                        });
                        Log($"Gamespace started for challenge {challenge.Id}.", gameId);

                        // record the created challenge/state for reporting
                        // request.Context.GamespacesStarted.Add(challengeState);

                        // transform info about the VMs so we can return them later, then report progress and move on
                        var gamespace = ChallengeStateToTeamGamespace(challengeState);
                        deployedGamespaces.Add(gamespace.Id, gamespace);

                        Log($"Challenge {gamespace.Id} has {gamespace.VmUris.Count()} VM(s): {string.Join(", ", challengeState.Vms.Select(vm => vm.Name))}", gameId);
                        // await _gameHubBus.SendExternalGameGamespacesDeployProgressChange(request.Context.ToUpdate());

                        // return the engine state of the challenge
                        return challengeState;
                    }
                    catch (GamespaceStartFailure ex)
                    {
                        Log($"Gamespace failed to start for challenge {challenge.Id} ({ex.Message}).", gameId);

                        var failedGamespace = new GameResourcesDeployGamespace
                        {
                            Id = challenge.Id,
                            IsDeployed = false,
                            VmUris = Array.Empty<string>()
                        };

                        deployedGamespaces.Add(failedGamespace.Id, failedGamespace);
                        failedDeployGamespaceIds.Add(failedGamespace.Id);
                        return null;
                    }
                });

                // fire a thread for each task in the batch. The task returns null if an error is thrown,
                // so check for that
                var deployResults = await Task.WhenAll(batchTasks.ToArray());

                // after the asynchronous part is over, we need to do database updates to ensure the DB has the correct 
                // game-engine-supplied state for each challenge
                foreach (var state in deployResults.Where(state => state is not null))
                {
                    string serializedState = null;
                    var isGamespaceOn = false;

                    if (state is not null)
                    {
                        serializedState = _jsonService.Serialize(state);
                        isGamespaceOn = state.IsActive && state.Vms is not null && state.Vms.Any();
                    }

                    await _store
                        .WithNoTracking<Data.Challenge>()
                        .Where(c => c.Id == state.Id)
                        .ExecuteUpdateAsync
                        (
                            up => up
                                .SetProperty(c => c.HasDeployedGamespace, isGamespaceOn)
                                .SetProperty(c => c.State, serializedState),
                            cancellationToken
                        );

                    Log($"Updated gamespace states for challenge {state.Id}. Gamespace on?: {isGamespaceOn}", gameId);
                }

                Log($"Finished {deployResults.Length} tasks done for gamespace batch #{batchIndex}.", gameId);
            }
        }

        // notify and return
        var totalGamespaceCount = deployedGamespaces.Count;
        var totalVmCount = deployedGamespaces.Values.SelectMany(gamespace => gamespace.VmUris).Count();
        var deployedGamespaceCount = deployedGamespaces.Values.Select(gamespace => gamespace.IsDeployed).Count();

        Log($"Finished deploying gamespaces: {totalGamespaceCount} gamespaces ({deployedGamespaceCount} ready), {totalVmCount} visible VMs.", gameId);
        Log($"Undeployed/unstarted gamespaces: {string.Join(", ", failedDeployGamespaceIds)}", gameId);
        // await _gameHubBus.SendExternalGameGamespacesDeployEnd(request.Context.ToUpdate());
        return new GameResourcesDeployGamespacesResult
        {
            FailedGamespaceDeployIds = failedDeployGamespaceIds,
            Gamespaces = deployedGamespaces,
        };
    }

    private GameResourcesDeployGamespace ChallengeStateToTeamGamespace(GameEngineGameState state)
        => new()
        {
            Id = state.Id,
            IsDeployed = state.IsActive,
            VmUris = _gameEngineService.GetGamespaceVms(state).Select(vm => vm.Url)
        };

    internal Task<string[]> ResolveChallengeSpecIds(string gameId, CancellationToken cancellationToken)
        => _store
            .WithNoTracking<Data.ChallengeSpec>()
            .Where(cs => cs.GameId == gameId)
            .Where(cs => !cs.Disabled)
            .Select(cs => cs.Id)
            .ToArrayAsync(cancellationToken);

    private void Log(string message, string gameId)
        => _logger.LogInformation(message: $"[RESOURCE DEPLOY :: {gameId}] {message}");
}
