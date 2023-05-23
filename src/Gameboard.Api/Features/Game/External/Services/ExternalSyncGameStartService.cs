using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Gameboard.Api.Common;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Data.Abstractions;
using Gameboard.Api.Features.GameEngine;
using Gameboard.Api.Features.Teams;
using Gameboard.Api.Services;
using Gameboard.Api.Structure.MediatR;
using Microsoft.Extensions.Logging;

namespace Gameboard.Api.Features.Games.External;

internal class ExternalSyncGameStartService
{
    private readonly ChallengeService _challengeService;
    private readonly IGamebrainService _gamebrainService;
    private readonly IGameEngineService _gameEngineService;
    private readonly IGameService _gameService;
    private readonly IGameStore _gameStore;
    private readonly IJsonService _jsonService;
    private ILogger<ExternalSyncGameStartService> _logger;
    private readonly IMapper _mapper;
    private readonly INowService _now;
    private readonly PlayerService _playerService;
    private readonly ITeamService _teamService;
    private readonly IValidatorService<ExternalSyncGameStartRequest> _validator;

    public ExternalSyncGameStartService
    (
        ChallengeService challengeService,
        IGamebrainService gamebrainService,
        IGameEngineService gameEngineService,
        IGameService gameService,
        IGameStore gameStore,
        IJsonService jsonService,
        ILogger<ExternalSyncGameStartService> logger,
        IMapper mapper,
        INowService now,
        PlayerService playerService,
        ITeamService teamService,
        IValidatorService<ExternalSyncGameStartRequest> validator
    )
    {
        _challengeService = challengeService;
        _gamebrainService = gamebrainService;
        _gameEngineService = gameEngineService;
        _gameService = gameService;
        _gameStore = gameStore;
        _jsonService = jsonService;
        _logger = logger;
        _mapper = mapper;
        _now = now;
        _playerService = playerService;
        _teamService = teamService;
        _validator = validator;
    }

    public async Task<ExternalGameStartMetaData> Start(ExternalSyncGameStartRequest request)
    {
        Log("Validating external / sync-start game request...", request.GameId);
        _validator.AddValidator(async (req, ctx) =>
        {
            // just do exists here since we need the game for other checks anyway
            var game = await _gameStore.Retrieve(req.GameId);
            if (game == null)
            {
                ctx.AddValidationException(new ResourceNotFound<Data.Game>(req.GameId));
                return;
            }

            if (!game.RequireSynchronizedStart)
                ctx.AddValidationException(new GameIsNotSyncStart(game.Id, $"""{nameof(ExternalSyncGameStartService)} can't start this game because it's not sync-start."""));

            if (game.Mode != GameMode.External)
                ctx.AddValidationException(new GameModeIsntExternal(game.Id, $"""{nameof(ExternalSyncGameStartService)} can't start this game because it's not an external game."""));
        });

        _validator.AddValidator(async (req, ctx) =>
        {
            var syncStartState = await _gameService.GetSyncStartState(req.GameId);

            if (!syncStartState.IsReady)
                ctx.AddValidationException(new CantStartNonReadySynchronizedGame(syncStartState));
        });
        await _validator.Validate(request);
        Log("Validation complete.", request.GameId);

        Log("Gathering data...", request.GameId);
        var game = await _gameStore.Retrieve(request.GameId);
        var specs = await _gameService.RetrieveChallengeSpecs(request.GameId);
        var players = _mapper.Map<IEnumerable<Api.Player>>(await _playerService.List(new PlayerDataFilter { gid = request.GameId }));
        var teams = players.GroupBy(p => p.TeamId).ToDictionary(g => g.Key, g => _teamService.ResolveCaptain(g.Key, g.ToList()));
        var teamDeployedChallenges = new Dictionary<string, List<Api.Challenge>>();
        var challengeGamespaces = new Dictionary<string, ExternalGameStartTeamGamespace>();
        Log($"Data gathered: {players.Count()} players on {teams.Keys.Count()}.", request.GameId);

        // this context holds everything we create along the way so we can try to roll it back upon exception
        var deployContext = new ExternalSyncGameDeployContext()
        {
            DeployedChallenges = new List<Challenge>(),
            DeployedGamespaces = new List<GameEngineGameState>(),
            TeamIds = teams.Keys
        };

        try
        {
            Log("Identifying team captains...", request.GameId);
            // validate that we have a team captain for every team before we do anything
            foreach (var teamId in teams.Keys)
            {
                if (teams[teamId] == null)
                    throw new CaptainResolutionFailure(teamId, "Couldn't resolve captain during external sync game start.");
            }

            Log("Deploying challenges...", request.GameId);
            // deploy all challenges
            foreach (var teamId in teams.Keys)
            {
                // hold onto each team's challenges
                Log($"""Deploying challenges for team "{teamId}".""", request.GameId);
                teamDeployedChallenges.Add(teamId, new List<Challenge>());

                foreach (var specId in specs.Select(s => s.Id))
                {
                    Log($"""Creating challenge for spec "{specId}"...""", request.GameId);

                    var challenge = await _challengeService.Create
                    (
                        new NewChallenge
                        {
                            PlayerId = teams[teamId].Id,
                            SpecId = specId,
                            StartGamespace = false, // hold gamespace startup until we're sure we've created all challenges
                            Variant = 0
                        },
                        teams.Values.First().Id, // for now, actor is first captain
                        _challengeService.BuildGraderUrl()
                    );

                    deployContext.DeployedChallenges.Add(challenge);
                    teamDeployedChallenges[teamId].Add(challenge);
                    Log($"Spec created.", request.GameId);
                }
            }

            // start all gamespaces
            Log("Deploying gamespaces...", request.GameId);
            foreach (var deployedChallenge in deployContext.DeployedChallenges)
            {
                _logger.LogInformation($"""Starting gamespace for challenge "{deployedChallenge.Id}" (teamId "{deployedChallenge.TeamId}")... """);
                var challengeState = await _gameEngineService.StartGamespace(deployedChallenge);
                _logger.LogInformation($"""Gamespace started for challenge "{deployedChallenge.Id}".""");

                var vms = _gameEngineService.GetGamespaceVms(challengeState);
                challengeGamespaces.Add(deployedChallenge.Id, new ExternalGameStartTeamGamespace
                {
                    Id = challengeState.Id,
                    Challenge = new SimpleEntity { Id = deployedChallenge.Id, Name = deployedChallenge.Name },
                    VmUrls = vms.Select(vm => vm.Url)
                });
                deployContext.DeployedGamespaces.Add(challengeState);
            }

            // establish all sessions
            Log("Starting a synchronized session for all teams...", request.GameId);
            var syncStartState = await _gameService.StartSynchronizedSession(game.Id);
            Log("Synchronized session started!", request.GameId);

            // TODO: notify gameboard to move players along

            // NOTIFY EXTERNAL CLIENT
            // build team objects to return
            var teamsToReturn = new List<ExternalGameStartMetaDataTeam>();
            foreach (var teamId in teams.Keys)
            {
                var teamChallengeIds = teamDeployedChallenges[teamId].Select(c => c.Id);
                var teamGameStates = deployContext.DeployedGamespaces.Where(g => teamChallengeIds.Contains(g.Id));

                var teamToReturn = new ExternalGameStartMetaDataTeam
                {
                    Id = teamId,
                    Name = teams[teamId].ApprovedName,
                    Gamespaces = teamChallengeIds.Select(cid => challengeGamespaces[cid])
                };

                teamsToReturn.Add(teamToReturn);
            }

            var retVal = new ExternalGameStartMetaData
            {
                Game = new SimpleEntity { Id = game.Id, Name = game.Name },
                Session = new ExternalGameStartMetaDataSession
                {
                    Now = _now.Get(),
                    SessionBegin = syncStartState.SessionBegin,
                    SessionEnd = syncStartState.SessionEnd
                },
                Teams = teamsToReturn
            };

            var metadataJson = _jsonService.Serialize(retVal);
            Log("Final metadata payload is here:", game.Id);
            Log(metadataJson, game.Id);

            Log("Notifying Gamebrain...", game.Id);
            await _gamebrainService.StartV2Game(retVal);
            Log("Gamebrain notified!", game.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError($"""Error during external sync game deploy: "{ex.Message}". Attempting cleanup...""");
            await TryCleanupFailedDeploy(deployContext);
            _logger.LogError("""Cleanup complete.""");
        }

        return null;
    }

    private void Log(string message, string gameId)
    {
        var prefix = $"""[EXTERNAL / SYNC - START GAME "{gameId}"] - {_now.Get()} - """;
        _logger.LogInformation($"{prefix} {message}");
    }

    private Task TryCleanupFailedDeploy(ExternalSyncGameDeployContext ctx)
    {
        // TODO
        return Task.CompletedTask;
    }
}
