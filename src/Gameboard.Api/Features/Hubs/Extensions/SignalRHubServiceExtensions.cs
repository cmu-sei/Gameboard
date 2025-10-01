// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Gameboard.Api.Structure.Logging;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Gameboard.Api.Hubs;

public static class SignalRHubServiceExtensions
{
    public static string GetUserLogContextString(this IGameboardHub hub)
        => $"[{hub.Context.User.FindFirstValue("name")} // {hub.Context.UserIdentifier} // {hub.Context.ConnectionId}]:";

    public static Task JoinGroup(this IGameboardHub hub, string groupIdentifier)
        => hub.Groups.AddToGroupAsync(hub.Context.ConnectionId, GetCanonicalGroupId(hub, groupIdentifier));

    public static Task LeaveGroup(this IGameboardHub hub, string groupIdentifier)
        => hub.Groups.RemoveFromGroupAsync(hub.Context.ConnectionId, GetCanonicalGroupId(hub, groupIdentifier));

    public static void LogOnConnected(this IGameboardHub hub, ILogger logger, HubCallerContext hubContext)
        => logger.LogInformation(LogEventId.Hub_Connection_Connected, message: $""" Connection id "{hubContext.ConnectionId}" started for user "{hubContext.UserIdentifier}." """);

    public static void LogOnDisconnected(this IGameboardHub hub, ILogger logger, HubCallerContext hubContext, Exception ex = null)
        => logger.LogInformation(LogEventId.Hub_Connection_Connected, ex, message: $"""Connection id "{hubContext.ConnectionId}" started for user {hubContext.UserIdentifier}""");

    public static string GetCanonicalGroupId(this ICanonicalGroupIdProvider hub, string groupIdentifier)
        => $"{hub.GroupType.ToString().ToLower()}-{groupIdentifier}";

    public static THubEvents SendToAllInGroupExcept<THub, THubEvents>(this IHubContext<THub, THubEvents> hubContext, ICanonicalGroupIdProvider hubBus, string groupIdentifier, params string[] exceptConnnectionIds)
        where THub : Hub<THubEvents>
        where THubEvents : class
        => hubContext.Clients.GroupExcept(hubBus.GetCanonicalGroupId(groupIdentifier), exceptConnnectionIds);

    public static THubEvents SendToGroup<THub, THubEvents>(this IHubContext<THub, THubEvents> hubContext, ICanonicalGroupIdProvider hubBus, string groupIdentifier)
        where THub : Hub<THubEvents>
        where THubEvents : class
        => hubContext.Clients.Group(hubBus.GetCanonicalGroupId(groupIdentifier));
}
