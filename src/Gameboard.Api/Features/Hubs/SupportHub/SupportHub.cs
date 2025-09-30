// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Gameboard.Api.Hubs;

[Authorize(AppConstants.HubPolicy)]
public sealed class SupportHub(
    ILogger<SupportHub> logger
    ) : Hub<ISupportHubEvent>, IGameboardHub
{
    private readonly ILogger<SupportHub> _logger = logger;
    internal readonly static string GROUP_STAFF = "staff";

    public GameboardHubType GroupType => GameboardHubType.Support;

    public override async Task OnConnectedAsync()
    {
        await base.OnConnectedAsync();
        this.LogOnConnected(_logger, Context);

        // join a personal channel for things like updates on specific tickets
        await Groups.AddToGroupAsync(Context.ConnectionId, Context.UserIdentifier);
    }

    public async override Task OnDisconnectedAsync(Exception exception)
    {
        this.LogOnDisconnected(_logger, Context, exception);
        await base.OnDisconnectedAsync(exception);
    }
}
