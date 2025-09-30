// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.


using System;
using System.Threading.Tasks;
using Gameboard.Api.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Gameboard.Api.Features.Games;

public interface IGameHubApi { }

[Authorize(AppConstants.HubPolicy)]
public class GameHub(ILogger<GameHub> logger) : Hub<IGameHubEvent>, IGameHubApi, IGameboardHub
{
    private readonly ILogger<GameHub> _logger = logger;

    public GameboardHubType GroupType => GameboardHubType.Game;

    public override async Task OnConnectedAsync()
    {
        await base.OnConnectedAsync();
        this.LogOnConnected(_logger, Context);
    }

    public override Task OnDisconnectedAsync(Exception ex)
    {
        this.LogOnDisconnected(_logger, Context, ex);
        return base.OnDisconnectedAsync(ex);
    }
}
