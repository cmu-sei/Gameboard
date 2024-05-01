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

    public ExternalGameStartService
    (
        IExternalGameService externalGameService,
        ILogger<ExternalGameStartService> logger,
        IGameHubService gameHubService,
        IGameResourcesDeploymentService gameResourcesDeploy,
        INowService nowService
    )
    {
        _externalGameService = externalGameService;
        _logger = logger;
        _gameHubService = gameHubService;
        _gameResourcesDeploy = gameResourcesDeploy;
        _nowService = nowService;
    }

    public TeamSessionResetType StartFailResetType => TeamSessionResetType.PreserveChallenges;

    public async Task<GameResourcesDeployResults> DeployResources(GameModeStartRequest request, CancellationToken cancellationToken)
    {
        var teamIds = request.Context.Teams.Select(t => t.Team.Id);
        return await _gameResourcesDeploy.DeployResources(teamIds, cancellationToken);
    }

    public Task<GamePlayState> GetGamePlayState(string gameId, CancellationToken cancellationToken)
    {
        return Task.FromResult(GamePlayState.Started);
    }

    public async Task<GameStartContext> Start(GameModeStartRequest request, CancellationToken cancellationToken)
    {
        var teamIds = request.Context.Teams.Select(t => t.Team.Id).ToArray();
        var resources = await _gameResourcesDeploy.DeployResources(teamIds, cancellationToken);

        var retVal = new GameStartContext
        {
            Game = request.Game,
            SpecIds = resources.TeamChallenges.SelectMany(kv => kv.Value).Select(c => c.SpecId).Distinct().ToArray(),
            StartTime = _nowService.Get(),
            TotalChallengeCount = resources.TeamChallenges.SelectMany(kv => kv.Value).Count(),
            TotalGamespaceCount = resources.TeamChallenges.SelectMany(kv => kv.Value).Count(),
        };

        retVal.ChallengesCreated.AddRange(request.Context.ChallengesCreated);
        retVal.Error = resources.DeployFailedGamespaceIds.Any() ? $"Gamespaces failed to deploy: {string.Join(',', resources.DeployFailedGamespaceIds)}" : null;
        retVal.GamespacesStarted.AddRange(request.Context.GamespacesStarted);
        retVal.GamespaceIdsStartFailed.AddRange(resources.DeployFailedGamespaceIds);
        retVal.Players.AddRange(request.Context.Players);
        retVal.Teams.AddRange(request.Context.Teams);

        return retVal;
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
        return Task.CompletedTask;
    }

    private void Log(string message, string gameId)
    {
        var prefix = $"[EXTERNAL / SYNC-START GAME {gameId}] - ";
        _logger.LogInformation(message: $"{prefix} {message}");
    }
}
