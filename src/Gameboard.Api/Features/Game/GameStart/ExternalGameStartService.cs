using System;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Features.Games.Start;
using Gameboard.Api.Features.Teams;
using Microsoft.Extensions.Logging;

namespace Gameboard.Api.Features.Games.External;

public interface IExternalGameStartService : IGameModeStartService { }

internal class ExternalGameStartService : IExternalGameStartService
{
    private readonly ILogger<ExternalGameStartService> _logger;
    private readonly IGameHubService _gameHubService;

    public ExternalGameStartService(ILogger<ExternalGameStartService> logger, IGameHubService gameHubService)
    {
        _logger = logger;
        _gameHubService = gameHubService;
    }

    public TeamSessionResetType StartFailResetType => TeamSessionResetType.PreserveChallenges;

    public Task<GameResourcesDeployResults> DeployResources(GameModeStartRequest request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<GamePlayState> GetGamePlayState(string gameId, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<GameStartContext> Start(GameModeStartRequest request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public async Task TryCleanUpFailedDeploy(GameModeStartRequest request, Exception exception, CancellationToken cancellationToken)
    {
        // log the error
        var exceptionMessage = $"""EXTERNAL GAME LAUNCH FAILURE (game "{request.Game.Id}"): {exception.GetType().Name} :: {exception.Message}""";
        Log(exceptionMessage, request.Game.Id);
        request.Context.Error = exceptionMessage;

        // notify the teams that something is amiss
        await _gameHubService.SendExternalGameLaunchFailure(request.Context.ToUpdate());
    }

    public Task ValidateStart(GameModeStartRequest request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    private void Log(string message, string gameId)
    {
        var prefix = $"[EXTERNAL / SYNC-START GAME {gameId}] - ";
        _logger.LogInformation(message: $"{prefix} {message}");
    }
}
