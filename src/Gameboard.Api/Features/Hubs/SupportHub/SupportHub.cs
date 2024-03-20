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
    public readonly static string GROUP_STAFF = "staff";

    public SupportHub
    (
        IActingUserService actingUserService,
        ILogger<SupportHub> logger
    )
    {
        _actingUserService = actingUserService;
        _logger = logger;
    }

    public GameboardHubType GroupType => GameboardHubType.SupportGlobal;

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

    public async Task JoinStaffGroup()
    {
        _logger.LogInformation(LogEventId.SupportHub_Staff_JoinStart, message: $"""User "{Context.UserIdentifier}" is joining the support staff group...""");

        // validate
        var user = _actingUserService.Get();

        if (!user.IsAdmin && !user.IsSupport)
            throw new ActionForbidden();

        // join
        await this.JoinGroup(GROUP_STAFF);

        _logger.LogInformation(LogEventId.SupportHub_Staff_JoinEnd, message: $"""User "{Context.UserIdentifier}" joined the support staff group.""");
    }
}
