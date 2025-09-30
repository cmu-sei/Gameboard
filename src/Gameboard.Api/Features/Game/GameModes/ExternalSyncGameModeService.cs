// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Data;
using Gameboard.Api.Features.GameEngine;
using Gameboard.Api.Features.Games.External;
using Gameboard.Api.Features.Teams;
using Gameboard.Api.Structure.MediatR;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Gameboard.Api.Features.Games;

public interface IExternalSyncGameModeService : IGameModeService { }

internal class ExternalSyncGameModeService : IExternalSyncGameModeService
{
    private readonly IExternalGameService _externalGameService;
    private readonly IExternalGameHostService _externalGameHostService;
    private readonly IGameEngineService _gameEngineService;
    private readonly IGameHubService _gameHubBus;
    private readonly IGameResourcesDeployService _gameResourcesDeployment;
    private readonly IJsonService _jsonService;
    private readonly ILogger<ExternalSyncGameModeService> _logger;
    private readonly IMapper _mapper;
    private readonly IMediator _mediator;
    private readonly INowService _now;
    private readonly IStore _store;
    private readonly ISyncStartGameService _syncStartGameService;
    private readonly ITeamService _teamService;
    private readonly IValidatorService<GameModeStartRequest> _validator;

    public ExternalSyncGameModeService
    (
        IExternalGameService externalGameService,
        IExternalGameHostService externalGameHostService,
        IGameEngineService gameEngineService,
        IGameHubService gameHubBus,
        IGameResourcesDeployService gameResourcesDeployment,
        IJsonService jsonService,
        ILogger<ExternalSyncGameModeService> logger,
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

    public bool DeployResourcesOnSessionStart { get => true; }

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

    public bool RequireSynchronizedSessions { get => true; }

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
                ctx.AddValidationException(new ExternalGameIsNotSyncStart(game.Id, $"""{nameof(ExternalSyncGameModeService)} can't start this game because it's not sync-start."""));

            if (game.Mode != GameEngineMode.External)
                ctx.AddValidationException(new GameModeIsntExternal(game.Id, $"""{nameof(ExternalSyncGameModeService)} can't start this game because it's not an external game."""));
        });

        _validator.AddValidator(async (req, ctx) =>
        {
            var syncStartState = await _syncStartGameService.GetSyncStartState(req.Game.Id, req.TeamIds, cancellationToken);

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

    public Task TryCleanUpFailedDeploy(GameModeStartRequest request, Exception exception, CancellationToken cancellationToken)
    {
        // log the error
        var exceptionMessage = $"""EXTERNAL GAME LAUNCH FAILURE (game "{request.Game.Id}"): {exception.GetType().Name} :: {exception.Message}""";
        Log(exceptionMessage, request.Game.Id);

        return Task.CompletedTask;
    }

    private void Log(string message, string gameId)
    {
        var prefix = $"[EXTERNAL / SYNC-START GAME {gameId}] - ";
        _logger.LogInformation(message: $"{prefix} {message}");
    }
}
