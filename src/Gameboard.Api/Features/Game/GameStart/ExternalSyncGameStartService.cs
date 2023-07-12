using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Gameboard.Api.Common;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Data.Abstractions;
using Gameboard.Api.Features.GameEngine;
using Gameboard.Api.Features.Games.Start;
using Gameboard.Api.Features.Teams;
using Gameboard.Api.Services;
using Gameboard.Api.Structure.MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Gameboard.Api.Features.Games.External;

public interface IExternalSyncGameStartService : IGameModeStartService { }

internal class ExternalSyncGameStartService : IExternalSyncGameStartService
{
    private readonly ChallengeService _challengeService;
    private readonly IStore<Data.ChallengeSpec> _challengeSpecStore;
    private readonly IGamebrainService _gamebrainService;
    private readonly IGameEngineService _gameEngineService;
    private readonly IGameHubBus _gameHubBus;
    private readonly IGameStore _gameStore;
    private readonly IJsonService _jsonService;
    private readonly ILockService _lockService;
    private readonly ILogger<ExternalSyncGameStartService> _logger;
    private readonly IMapper _mapper;
    private readonly IPlayerStore _playerStore;
    private readonly INowService _now;
    private readonly ISyncStartGameService _syncStartGameService;
    private readonly ITeamService _teamService;
    private readonly IValidatorService<GameModeStartRequest> _validator;

    public ExternalSyncGameStartService
    (
        ChallengeService challengeService,
        IStore<Data.ChallengeSpec> challengeSpecStore,
        IGamebrainService gamebrainService,
        IGameEngineService gameEngineService,
        IGameHubBus gameHubBus,
        IGameStore gameStore,
        IJsonService jsonService,
        ILockService lockService,
        ILogger<ExternalSyncGameStartService> logger,
        IMapper mapper,
        INowService now,
        IPlayerStore playerStore,
        ISyncStartGameService syncStartGameService,
        ITeamService teamService,
        IValidatorService<GameModeStartRequest> validator
    )
    {
        _challengeService = challengeService;
        _challengeSpecStore = challengeSpecStore;
        _gamebrainService = gamebrainService;
        _gameEngineService = gameEngineService;
        _gameHubBus = gameHubBus;
        _gameStore = gameStore;
        _jsonService = jsonService;
        _lockService = lockService;
        _logger = logger;
        _mapper = mapper;
        _now = now;
        _playerStore = playerStore;
        _syncStartGameService = syncStartGameService;
        _teamService = teamService;
        _validator = validator;
    }

    public async Task ValidateStart(GameModeStartRequest request)
    {
        _logger.LogInformation("Validating external / sync-start game request...", request.GameId);

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
            var syncStartState = await _syncStartGameService.GetSyncStartState(req.GameId);

            if (!syncStartState.IsReady)
                ctx.AddValidationException(new CantStartNonReadySynchronizedGame(syncStartState));
        });

        await _validator.Validate(request);
        _logger.LogInformation("Validation complete.", request.GameId);
    }

    public async Task<GameStartState> Start(GameModeStartRequest request)
    {
        try
        {
            Log("Gathering data...", request.GameId);
            await _gameHubBus.SendExternalGameLaunchStart(request.State);

            // update the game start/end time to now so we can reason better about whether the game has actually
            // started launching or not
            await _gameStore
                .List()
                .Where(g => g.Id == request.GameId)
                .ExecuteUpdateAsync
                (
                    g => g
                        .SetProperty(g => g.GameStart, request.State.StartTime)
                        .SetProperty(g => g.GameEnd, DateTimeOffset.MinValue)
                );

            Log("Deploying challenges...", request.GameId);
            var teamDeployedChallenges = new Dictionary<string, List<Api.Challenge>>();
            var challengeGamespaces = new Dictionary<string, ExternalGameStartTeamGamespace>();

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
                        _challengeService.BuildGraderUrl()
                    );

                    request.State.ChallengesCreated.Add(_mapper.Map<GameStartStateChallenge>(challenge));
                    teamDeployedChallenges[team.Team.Id].Add(challenge);
                    Log($"Spec instantiated for team {team.Team.Id}.", request.GameId);

                    await _gameHubBus.SendExternalGameChallengesDeployProgressChange(request.State);
                }
            }

            await _gameHubBus.SendExternalGameChallengesDeployEnd(request.State);

            // start all gamespaces
            Log("Deploying gamespaces...", request.GameId);
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

                var vms = _gameEngineService.GetGamespaceVms(challengeState);
                challengeGamespaces.Add(deployedChallenge.Challenge.Id, new ExternalGameStartTeamGamespace
                {
                    Id = challengeState.Id,
                    Challenge = deployedChallenge.Challenge,
                    VmUrls = vms.Select(vm => vm.Url)
                });

                request.State.GamespacesDeployed.Add(challengeState);
                await _gameHubBus.SendExternalGameGamespacesDeployProgressChange(request.State);
            }
            await _gameHubBus.SendExternalGameGamespacesDeployEnd(request.State);

            // establish all sessions
            _logger.LogInformation("Starting a synchronized session for all teams...", request.GameId);
            var syncGameStartState = await _syncStartGameService.StartSynchronizedSession(request.GameId);
            _logger.LogInformation("Synchronized session started!", request.GameId);

            // build metadata for external host
            var metaData = BuildExternalGameMetaData(request.State, syncGameStartState);

            // NOTIFY EXTERNAL CLIENT
            _logger.LogInformation("Notifying Gamebrain...");
            var externalClientTeamConfigs = await _gamebrainService.StartV2Game(metaData);
            _logger.LogInformation("Gamebrain notified!");

            // notify gameboard to move players along
            await _gameHubBus.SendSyncStartGameStarting(syncGameStartState);

            // update game end time after deployment
            var gameEndTime = request.State.StartTime.AddMinutes(request.Context.SessionLengthMinutes);
            await _gameStore
                .List()
                .Where(g => g.Id == request.GameId)
                .ExecuteUpdateAsync(g => g.SetProperty(g => g.GameEnd, gameEndTime));

            // assign each team a headless Url from gamebrain's response
            foreach (var team in request.State.Teams)
            {
                var config = externalClientTeamConfigs.FirstOrDefault(t => t.TeamID == team.Team.Id);
                if (config is null)
                    _logger.LogError($"""Team "{team.Team.Id}" wasn't assigned a headless URL by Gamebrain.""");
                else
                    team.HeadlessUrl = config.HeadlessServerUrl;
            }

            // on we go
            await _gameHubBus.SendExternalGameLaunchEnd(request.State);
        }
        catch (Exception ex)
        {
            var exceptionMessage = $"""EXTERNAL GAME LAUNCH FAILURE (game "{request.GameId}"): {ex.GetType().Name} :: {ex.Message}""";
            _logger.LogError(message: exceptionMessage);
            request.State.Error = exceptionMessage;
            await this._gameHubBus.SendExternalGameLaunchFailure(request.State);
        }

        return request.State;
    }

    public async Task<GameStartPhase> GetStartPhase(string gameId)
    {
        var game = await _gameStore.ListAsNoTracking().FirstOrDefaultAsync(g => g.Id == gameId);
        var hasStart = game.GameStart.IsNotEmpty();
        var hasEnd = game.GameEnd.IsNotEmpty();
        var now = _now.Get();

        if (game == null)
            throw new ResourceNotFound<Data.Game>(gameId);

        if (game.GameStart < now)
            return GameStartPhase.NotStarted;

        if (game.GameEnd < now && hasEnd)
            return GameStartPhase.GameOver;

        if (game.GameStart < now && game.GameEnd > now && hasEnd)
            return GameStartPhase.Started;

        return GameStartPhase.Starting;
    }

    private ExternalGameStartMetaData BuildExternalGameMetaData(GameStartState startState, SyncStartGameStartedState syncgameStartState)
    {
        // build team objects to return
        var teamsToReturn = new List<ExternalGameStartMetaDataTeam>();
        foreach (var team in startState.Teams)
        {
            var teamChallenges = startState.ChallengesCreated.Where(c => c.TeamId == team.Team.Id).Select(c => c.Challenge);
            var teamGameStates = startState.GamespacesDeployed.Where(g => teamChallenges.Select(c => c.Id).Contains(g.Id));

            var teamToReturn = new ExternalGameStartMetaDataTeam
            {
                Id = team.Team.Id,
                Name = team.Team.Name,
                Gamespaces = teamGameStates.Select(gs => new ExternalGameStartTeamGamespace
                {
                    Id = gs.Id,
                    Challenge = teamChallenges.First(c => c.Id == gs.Id),
                    VmUrls = _gameEngineService.GetGamespaceVms(gs).Select(vm => vm.Url)
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

    private void Log(string message, string gameId)
    {
        var prefix = $"""[EXTERNAL / SYNC - START GAME "{gameId}"] - {_now.Get()} - """;
        _logger.LogInformation(message: $"{prefix} {message}");
    }
}
