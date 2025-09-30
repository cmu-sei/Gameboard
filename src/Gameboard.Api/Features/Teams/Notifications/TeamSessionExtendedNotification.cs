// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Games.External;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Teams;

public record TeamSessionExtendedNotification(string TeamId, DateTimeOffset NewSessionEnd) : INotification;

internal class TeamSessionExtendedHandler : INotificationHandler<TeamSessionExtendedNotification>
{
    private readonly IExternalGameHostService _gamebrainService;
    private readonly IStore _store;
    private readonly ITeamService _teamService;

    public TeamSessionExtendedHandler
    (
        IExternalGameHostService gamebrainService,
        IStore store,
        ITeamService teamService
    )
    {
        _gamebrainService = gamebrainService;
        _store = store;
        _teamService = teamService;
    }

    public async Task Handle(TeamSessionExtendedNotification notification, CancellationToken cancellationToken)
    {
        var gameId = await _teamService.GetGameId(notification.TeamId, cancellationToken);
        var gameMode = await _store
            .WithNoTracking<Data.Game>()
            .Where(g => g.Id == gameId)
            .Select(g => g.Mode)
            .SingleOrDefaultAsync(cancellationToken);

        if (gameMode == GameEngineMode.External)
            await _gamebrainService.ExtendTeamSession(notification.TeamId, notification.NewSessionEnd, cancellationToken);
    }
}
