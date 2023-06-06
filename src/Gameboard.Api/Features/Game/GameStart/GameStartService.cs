using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Gameboard.Api.Common;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Data.Abstractions;
using Gameboard.Api.Features.GameEngine;
using Gameboard.Api.Features.Games.External;
using Gameboard.Api.Services;
using Gameboard.Api.Structure;
using Gameboard.Api.Structure.MediatR;
using Microsoft.Extensions.Logging;

namespace Gameboard.Api.Features.Games;

public interface IGameStartService
{
    Task HandleSyncStartStateChanged(SyncGameStartRequest request);
    Task<ExternalGameStartMetaData> Start(GameStartRequest request);
}

internal class GameStartService : IGameStartService
{
    private readonly IGameEngineService _gameEngineService;
    private readonly IGamebrainService _gamebrainService;
    private readonly IGameHubBus _gameHubBus;
    private readonly IGameService _gameService;
    private readonly IGameStore _gameStore;
    private readonly IJsonService _jsonService;
    private ILogger<GameStartService> _logger;
    private readonly INowService _now;
    private readonly IPlayerStore _playerStore;
    private readonly IExternalSyncGameStartService _externalSyncGameStartService;
    private readonly ISyncStartGameService _syncStartGameService;
    private readonly IValidatorService<GameStartRequest> _validator;

    public GameStartService
    (
        IExternalSyncGameStartService externalSyncGameStartService,
        IGamebrainService gamebrainService,
        IGameEngineService gameEngineService,
        IGameHubBus gameHubBus,
        IGameService gameService,
        IGameStore gameStore,
        IJsonService jsonService,
        ILogger<GameStartService> logger,
        INowService now,
        IPlayerStore playerStore,
        ISyncStartGameService syncGameStartService,
        IValidatorService<GameStartRequest> validator
    )
    {
        _externalSyncGameStartService = externalSyncGameStartService;
        _gamebrainService = gamebrainService;
        _gameEngineService = gameEngineService;
        _gameHubBus = gameHubBus;
        _gameService = gameService;
        _gameStore = gameStore;
        _jsonService = jsonService;
        _logger = logger;
        _now = now;
        _playerStore = playerStore;
        _syncStartGameService = syncGameStartService;
        _validator = validator;
    }

    public async Task<ExternalGameStartMetaData> Start(GameStartRequest request)
    {
        var game = await _gameStore.Retrieve(request.GameId);
        var ctx = new GameStartContext
        {
            Game = new SimpleEntity { Id = game.Id, Name = game.Name },
            DeployedChallenges = new List<Challenge>(),
            DeployedGamespaces = new List<GameEngineGameState>(),
            Teams = new List<SimpleEntity>()
        };

        // three cases to be accommodated: standard challenges, sync start + external (unity), and
        // non-sync-start + external (cubespace)
        // if (game.Mode == GameMode.Standard && !game.RequireSynchronizedStart)
        // {
        //     if (actingUser == null)
        //         throw new CantStartStandardGameWithoutActingUserParameter(gameId);

        //     await _mediator.Send(new StartStandardNonSyncGameCommand(gameId, actingUser));
        // }

        try
        {
            if (game.Mode == GameMode.External && game.RequireSynchronizedStart)
            {
                await ValidateExternalSyncGame(request);

                await _externalSyncGameStartService.Start(new ExternalSyncGameStartRequest
                {
                    Context = ctx,
                    GameId = request.GameId
                });

                // establish all sessions
                _logger.LogInformation("Starting a synchronized session for all teams...", request.GameId);
                var syncGameStartState = await _syncStartGameService.StartSynchronizedSession(game.Id);
                _logger.LogInformation("Synchronized session started!", request.GameId);

                // build metadata for external host
                var metaData = BuildMetaData(ctx, syncGameStartState);

                // NOTIFY EXTERNAL CLIENT
                _logger.LogInformation("Notifying Gamebrain...");
                await _gamebrainService.StartV2Game(metaData);
                _logger.LogInformation("Gamebrain notified!");

                // notify gameboard to move players along
                await this._gameHubBus.SendSyncStartGameStarting(syncGameStartState);

                return metaData;
            }

            // other combinations of game mode/sync start
            throw new System.NotImplementedException();
        }
        catch (Exception ex)
        {
            _logger.LogError(LogEventId.GameStart_Failed, exception: ex, message: $"""Deploy for game "{game.Id}" """);
            await TryCleanupFailedDeploy(ctx);
        }

        return null;
    }

    public async Task HandleSyncStartStateChanged(SyncGameStartRequest request)
    {
        var state = await _syncStartGameService.GetSyncStartState(request.GameId);
        await _gameHubBus.SendSyncStartGameStateChanged(state);

        // IFF everyone is ready, start all sessions and return info about them
        if (!state.IsReady)
            return;

        // for now, we're assuming the "happy path" of sync start games being external games, but we'll separate them later
        // var session = await StartSynchronizedSession(gameId); ;
        await Start(new GameStartRequest { GameId = request.GameId });
    }

    private ExternalGameStartMetaData BuildMetaData(GameStartContext ctx, SyncStartGameStartedState syncgameStartState)
    {
        // build team objects to return
        var teamsToReturn = new List<ExternalGameStartMetaDataTeam>();
        foreach (var team in ctx.Teams)
        {
            var teamChallenges = ctx.DeployedChallenges.Where(c => c.TeamId == team.Id).Select(c => new SimpleEntity { Id = c.Id, Name = c.Name });
            var teamGameStates = ctx.DeployedGamespaces.Where(g => teamChallenges.Select(c => c.Id).Contains(g.Id));

            var teamToReturn = new ExternalGameStartMetaDataTeam
            {
                Id = team.Id,
                Name = team.Name,
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
            Game = ctx.Game,
            Session = new ExternalGameStartMetaDataSession
            {
                Now = _now.Get(),
                SessionBegin = syncgameStartState.SessionBegin,
                SessionEnd = syncgameStartState.SessionEnd
            },
            Teams = teamsToReturn
        };

        var metadataJson = _jsonService.Serialize(retVal);
        _logger.LogInformation($"""Final metadata payload for game "{retVal.Game.Id}" is here: {metadataJson}""");
        return retVal;
    }

    private async Task ValidateExternalSyncGame(GameStartRequest request)
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

    private Task TryCleanupFailedDeploy(GameStartContext ctx)
    {
        // TODO
        return Task.CompletedTask;
    }
}
