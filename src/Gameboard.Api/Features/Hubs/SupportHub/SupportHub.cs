using System;
using System.Threading.Tasks;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Structure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Gameboard.Api.Hubs;

[Authorize(AppConstants.HubPolicy)]
public sealed class SupportHub : Hub<ISupportHubEvent>, IGameboardHub
{
    private readonly IActingUserService _actingUserService;
    private readonly ILogger<SupportHub> _logger;
    internal readonly static string GROUP_STAFF = "staff";

    public SupportHub
    (
        IActingUserService actingUserService,
        ILogger<SupportHub> logger
    )
    {
        _actingUserService = actingUserService;
        _logger = logger;
    }

    public GameboardHubType GroupType => GameboardHubType.Support;

    public override async Task OnConnectedAsync()
    {
        await base.OnConnectedAsync();
        this.LogOnConnected(_logger, Context);

        var user = _actingUserService.Get();

        // join a personal channel for things like updates on specific tickets
        await Groups.AddToGroupAsync(Context.ConnectionId, Context.UserIdentifier);
    }

    public async override Task OnDisconnectedAsync(Exception exception)
    {
        this.LogOnDisconnected(_logger, Context, exception);
        await base.OnDisconnectedAsync(exception);
    }
}
