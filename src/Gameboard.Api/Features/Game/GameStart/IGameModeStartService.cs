// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Features.Teams;

namespace Gameboard.Api.Features.Games.Start;

// Gameboard currently has four possible game modes:
//
// 1. Standard (VM) games, no synchronized start
// 2. Standard (VM) games, synchronized start
// 3. External (e.g. Unity) games, no synchronized start
// 4. External (e.g. Unity) games, synchronized start
// 
// Each possible mode will eventually have an implementation of this interface that orchestrates 
// the logic that happens when a game starts (e.g. ExternalSyncGameStartService for #4)
public interface IGameModeStartService
{
    /// <summary>
    /// The app will automatically reset the sessions of all registered teams on failure during Start. Indicate whether
    /// your mode wants a complete deletion of the team or another reset type.
    /// </summary>
    public TeamSessionResetType StartFailResetType { get; }

    /// <summary>
    /// This overload is only to check on the state of the game before enrolling a player or team - any checks
    /// on the game state for a specific team should use the GetGamePlayStateForTeam function.
    /// </summary>
    /// <param name="gameId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<GamePlayState> GetGamePlayState(string gameId, CancellationToken cancellationToken);

    /// <summary>
    /// Indicates the "play state" of a team. For example, in standard unsync'd games,
    /// this should return NotStarted if the execution window isn't open and should return GameOver if
    /// the game's execution window is closed.
    /// </summary>
    /// <param name="teamId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<GamePlayState> GetGamePlayStateForTeam(string teamId, CancellationToken cancellationToken);

    /// <summary>
    /// Starts the game. At the end of this function, a call to GetGamePlayState for the game should return "Started".
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task Start(GameModeStartRequest request, CancellationToken cancellationToken);

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
