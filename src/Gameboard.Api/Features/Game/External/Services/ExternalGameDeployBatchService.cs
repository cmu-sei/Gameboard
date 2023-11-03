using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Gameboard.Api.Features.GameEngine;
using Microsoft.Extensions.Logging;

namespace Gameboard.Api.Features.Games.External;

public interface IExternalGameDeployBatchService
{
    /// <summary>
    /// Create batches of gamespace deploy requests from challenges. 
    /// 
    /// An external game can have many challenges, and each challenge has an associated gamespace. For
    /// each gamespace, Gameboard must issue a request to the game engine that causes it to deploy
    /// the gamespace. Requesting all of these at once can cause issues with request timeouts, so
    /// we optionally allow Gameboard sysadmins to configure a batch size appropriate to their game engine.
    /// 
    /// By default, this value is 4, meaning that upon start of an external game, Gameboard will issue
    /// requests to the game engine in batches of 4 until all gamespaces have been deployed. Configure this
    /// with the Core__GameEngineDeployBatchSize setting in Gameboard's helm chart.
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    IEnumerable<IEnumerable<Task<GameEngineGameState>>> BuildDeployBatches(GameModeStartRequest request);
}

internal class ExternalGameDeployBatchService : IExternalGameDeployBatchService
{
    private readonly CoreOptions _coreOptions;
    private readonly IGameEngineService _gameEngine;
    private readonly IGameHubBus _gameHubBus;
    private readonly ILogger<ExternalGameDeployBatchService> _logger;

    public ExternalGameDeployBatchService
    (
        CoreOptions coreOptions,
        IGameEngineService gameEngine,
        IGameHubBus gameHubBus,
        ILogger<ExternalGameDeployBatchService> logger
    )
    {
        _coreOptions = coreOptions;
        _gameEngine = gameEngine;
        _gameHubBus = gameHubBus;
        _logger = logger;
    }

    public IEnumerable<IEnumerable<Task<GameEngineGameState>>> BuildDeployBatches(GameModeStartRequest request)
    {
        // first, create a task for each gamespace to be deployed
        var gamespaceTasks = request.State.ChallengesCreated.Select(async c =>
        {
            _logger.LogInformation(message: $"""Starting {c.GameEngineType} gamespace for challenge "{c.Challenge.Id}" (teamId "{c.TeamId}")...""");
            var challengeState = await _gameEngine.StartGamespace(new GameEngineGamespaceStartRequest
            {
                ChallengeId = c.Challenge.Id,
                GameEngineType = c.GameEngineType
            });

            request.State.GamespacesStarted.Add(challengeState);
            await _gameHubBus.SendExternalGameGamespacesDeployProgressChange(request.State);
            _logger.LogInformation(message: $"""Gamespace started for challenge "{c.Challenge.Id}".""");

            // keep the state given to us by the engine
            return challengeState;
        }).ToArray();

        // if the setting isn't configured or is a nonsense value, just return all the tasks in one batch
        if (_coreOptions.GameEngineDeployBatchSize <= 1)
            return new Task<GameEngineGameState>[][] { gamespaceTasks.ToArray() };

        // otherwise, create batches of the appropriate size plus an additional batch for any leftovers
        var batchList = new List<IEnumerable<Task<GameEngineGameState>>>();
        List<Task<GameEngineGameState>> currentBatch = null;

        for (var challengeIndex = 0; challengeIndex < gamespaceTasks.Length; challengeIndex++)
        {
            if (challengeIndex % _coreOptions.GameEngineDeployBatchSize == 0)
            {
                currentBatch = new List<Task<GameEngineGameState>>();
                batchList.Add(currentBatch);
            }

            currentBatch.Add(gamespaceTasks[challengeIndex]);
        }

        return batchList;
    }
}
