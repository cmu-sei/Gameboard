using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Features.Games.Start;
using Gameboard.Api.Features.Teams;
using Microsoft.Extensions.Logging;

namespace Gameboard.Api.Features.Games.External;

public interface IExternalGameStartService : IGameModeStartService { }

internal class ExternalGameStartService : IExternalGameStartService
{
    private readonly IExternalGameService _externalGameService;
    private readonly IGameHubService _gameHubService;
    private readonly IGameResourcesDeploymentService _gameResourcesDeploy;
    private readonly ILogger<ExternalGameStartService> _logger;
    private readonly INowService _nowService;
    private readonly ITeamService _teamService;

    public ExternalGameStartService
    (
        IExternalGameService externalGameService,
        ILogger<ExternalGameStartService> logger,
        IGameHubService gameHubService,
        IGameResourcesDeploymentService gameResourcesDeploy,
        INowService nowService,
        ITeamService teamService
    )
    {
        _externalGameService = externalGameService;
        _logger = logger;
        _gameHubService = gameHubService;
        _gameResourcesDeploy = gameResourcesDeploy;
        _nowService = nowService;
        _teamService = teamService;
    }

    public TeamSessionResetType StartFailResetType => TeamSessionResetType.PreserveChallenges;

    public Task<GameResourcesDeployResults> DeployResources(GameModeStartRequest request, CancellationToken cancellationToken)
        => _gameResourcesDeploy.DeployResources(request.TeamIds, cancellationToken);

    public Task<GamePlayState> GetGamePlayState(string gameId, CancellationToken cancellationToken)
        => GetGamePlayStateForGameAndTeam(gameId, null, cancellationToken);

    public Task<GamePlayState> GetGamePlayStateForTeam(string teamId, CancellationToken cancellationToken)
        => GetGamePlayStateForGameAndTeam(null, teamId, cancellationToken);

    private async Task<GamePlayState> GetGamePlayStateForGameAndTeam(string gameId, string teamId, CancellationToken cancellationToken)
    {
        if (teamId.IsNotEmpty())
            gameId = await _teamService.GetGameId(teamId, cancellationToken);
        var teamExternalGameState = await _externalGameService.GetTeam(teamId, cancellationToken);

        // this could be null if deployment hasn't started
        if (teamExternalGameState is null || teamExternalGameState.DeployStatus == ExternalGameDeployStatus.NotStarted)
            return GamePlayState.NotStarted;

        if (teamExternalGameState.DeployStatus == ExternalGameDeployStatus.Deploying)
            return GamePlayState.DeployingResources;

        if (teamExternalGameState.DeployStatus == ExternalGameDeployStatus.Deployed)
            return GamePlayState.Started;

        if (teamExternalGameState.DeployStatus == ExternalGameDeployStatus.PartiallyDeployed)
            return GamePlayState.Starting;

        throw new CantResolveGamePlayState(teamId, gameId);
    }

    public async Task Start(GameModeStartRequest request, CancellationToken cancellationToken)
    {
        Log($"Launching game {request.Game.Id} with {request.TeamIds.Count()} teams...", request.Game.Id);

        await _gameResourcesDeploy.DeployResources(request.TeamIds, cancellationToken);

        // update external host and get configuration information for teams
        await _externalGameService.Start(request.TeamIds, cancellationToken);

        // on we go
        Log("External (non-sync) game launched.", request.Game.Id);
    }

    public Task TryCleanUpFailedDeploy(GameModeStartRequest request, Exception exception, CancellationToken cancellationToken)
    {
        // log the error
        var exceptionMessage = $"""EXTERNAL GAME LAUNCH FAILURE (game "{request.Game.Id}"): {exception.GetType().Name} :: {exception.Message}""";
        Log(exceptionMessage, request.Game.Id);
        return Task.CompletedTask;
    }

    public Task ValidateStart(GameModeStartRequest request, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private void Log(string message, string gameId)
    {
        var prefix = $"[EXTERNAL / SYNC-START GAME {gameId}] - ";
        _logger.LogInformation(message: $"{prefix} {message}");
    }
}
