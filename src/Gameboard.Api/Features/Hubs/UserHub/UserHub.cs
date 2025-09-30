// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Threading.Tasks;
using Gameboard.Api.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Gameboard.Api.Features.Users;

public interface IUserHubApi { }

[Authorize(AppConstants.HubPolicy)]
public sealed class UserHub : Hub<IUserHubEvent>, IUserHubApi, IGameboardHub
{
    private readonly ILogger<UserHub> _logger;
    public UserHub
    (
        ILogger<UserHub> logger
    )
    {
        _logger = logger;
    }

    public GameboardHubType GroupType => GameboardHubType.User;

    public async override Task OnConnectedAsync()
    {
        await base.OnConnectedAsync();
        this.LogOnConnected(_logger, Context);
        await Groups.AddToGroupAsync(Context.ConnectionId, this.GetCanonicalGroupId(Context.UserIdentifier));
    }
}
