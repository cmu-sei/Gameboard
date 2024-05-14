using System;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Features.Teams;
using Microsoft.Extensions.Logging;

namespace Gameboard.Api.Features.Games.External;

public interface IExternalGameModeService : IGameModeService { }

internal class ExternalGameModeService : IExternalGameModeService
{
    private readonly IExternalGameService _externalGameService;
    private readonly ILogger<ExternalGameModeService> _logger;
    private readonly ITeamService _teamService;

    public ExternalGameModeService
    (
        IExternalGameService externalGameService,
        ILogger<ExternalGameModeService> logger,
        ITeamService teamService
    )
    {
        _externalGameService = externalGameService;
        _logger = logger;
        _teamService = teamService;
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

    public bool RequireSynchronizedSessions { get => false; }

    public TeamSessionResetType StartFailResetType => TeamSessionResetType.PreserveChallenges;

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
        var prefix = $"[EXTERNAL GAME {gameId}] - ";
        _logger.LogInformation(message: $"{prefix} {message}");
    }
}
