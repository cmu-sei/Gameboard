// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Threading.Tasks;
using Gameboard.Api.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Gameboard.Api.Features.Hubs;

public sealed class ScoreHub : Hub<IScoreHubEvent>, IGameboardHub
{
    private readonly ILogger<ScoreHub> _logger;
    private readonly IScoreHubBus _scoreHubBus;

    public ScoreHub
    (
        ILogger<ScoreHub> logger,
        IScoreHubBus scoreHubBus
    )
    {
        _logger = logger;
        _scoreHubBus = scoreHubBus;
    }

    public GameboardHubType GroupType => GameboardHubType.Score;

    public override async Task OnConnectedAsync()
    {
        this.LogOnConnected(_logger, Context);
        await Groups.AddToGroupAsync(Context.ConnectionId, this.GetCanonicalGroupId(Context.ConnectionId));
        await base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception exception)
    {
        this.LogOnDisconnected(_logger, Context, exception);
        return base.OnDisconnectedAsync(exception);
    }
}
