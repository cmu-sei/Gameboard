// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Features.Teams;

namespace Gameboard.Api.Features.Games;

public interface IGameModeService
{
    public bool DeployResourcesOnSessionStart { get; }

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

    public bool RequireSynchronizedSessions { get; }

    /// <summary>
    /// The app will automatically reset the sessions of all registered teams on failure during Start. Indicate whether
    /// your mode wants a complete deletion of the team or another reset type.
    /// </summary>
    public TeamSessionResetType StartFailResetType { get; }

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
