// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Teams;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Games;

public interface IStandardGameModeService : IGameModeService { }

internal class StandardGameModeService : IStandardGameModeService
{
    private readonly INowService _now;
    private readonly IStore _store;

    public StandardGameModeService(INowService now, IStore store)
    {
        _now = now;
        _store = store;
    }

    public bool DeployResourcesOnSessionStart => false;
    public bool RequireSynchronizedSessions => false;

    public TeamSessionResetType StartFailResetType => TeamSessionResetType.ArchiveChallenges;

    public Task<GamePlayState> GetGamePlayState(string gameId, CancellationToken cancellationToken)
        => Task.FromResult(GamePlayState.NotStarted);

    public Task<GamePlayState> GetGamePlayStateForTeam(string teamId, CancellationToken cancellationToken)
        => Task.FromResult(GamePlayState.NotStarted);

    public Task TryCleanUpFailedDeploy(GameModeStartRequest request, Exception exception, CancellationToken cancellationToken)
        => Task.CompletedTask;

    public Task ValidateStart(GameModeStartRequest request, CancellationToken cancellationToken)
        => Task.CompletedTask;
}
