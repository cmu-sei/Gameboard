using System;
using System.Threading;
using System.Threading.Tasks;

namespace Gameboard.Api.Features.Games.Start;

// Gameboard currently has four possible game modes:
//
// 1. Standard (VM) games, no synchronized start
// 2. Standard (VM) games, synchronized start
// 3. External (Unity) games, no synchronized start (e.g. PC4)
// 4. External (Unity) games, synchronized start
// 
// Each possible mode will eventually have an implementation of this interface that orchestrates 
// the logic that happens when a game starts (e.g. ExternalSyncGameStartService for #4)
public interface IGameModeStartService
{
    /// <summary>
    /// Indicates the playability of the given game for the given team. For example, in standard unsync'd games,
    /// this should return NotStarted if the execution window isn't open and should return GameOver if
    /// the game's execution window is closed or if the team's session for the game is over.
    /// </summary>
    /// <param name="gameId"></param>
    /// <param name="teamId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<GamePlayState> GetGamePlayState(string gameId, string teamId, CancellationToken cancellationToken);

    /// <summary>
    /// Deploy any resources (i.e. challenges, game engine gamespaces, etc.) for the game. Note that this happens
    /// automatically if the game is started on the server side through IGameModeStartService.Start. We expose it
    /// here to allow pre-deployment of games which need a lot of resources.
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<GameStartDeployedResources> DeployResources(GameModeStartRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Starts the game. At the end of this function, a call to GetGamePlayState for the game should return "Started".
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<GameStartContext> Start(GameModeStartRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Called when an exception is thrown during DeployResources or Start. Lets the game mode service clean up
    /// any resources that are specific to its game type.
    /// </summary>
    /// <param name="request"></param>
    /// <param name="exception"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task TryCleanUpFailedDeploy(GameModeStartRequest request, Exception exception, CancellationToken cancellationToken);

    /// <summary>
    /// Ensures that we have a usable start command.
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task ValidateStart(GameModeStartRequest request, CancellationToken cancellationToken);
}
