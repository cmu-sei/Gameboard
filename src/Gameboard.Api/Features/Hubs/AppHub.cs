// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Security.Claims;
using System.Threading.Tasks;
using AutoMapper;
using Gameboard.Api.Data.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Gameboard.Api.Hubs
{
    [Authorize(AppConstants.HubPolicy)]
    public class AppHub: Hub<IAppHubEvent>, IAppHubAction
    {
        ILogger Logger { get; }
        IPlayerStore PlayerStore { get; }
        IMapper Mapper { get; }
        const string ContextKey = "player";

        public AppHub(
            ILogger<AppHub> logger,
            IMapper mapper,
            IPlayerStore playerStore
        ) {
            Logger = logger;
            PlayerStore = playerStore;
            Mapper = mapper;
        }

        public override Task OnConnectedAsync()
        {
            Logger.LogDebug($"Session Connected: {Context.User.FindFirstValue("name")} {Context.UserIdentifier} {Context.ConnectionId}");
            return base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception ex)
        {
            Logger.LogDebug($"Session Disconnected: {Context.ConnectionId}");

            await base.OnDisconnectedAsync(ex);

            await Leave();
        }

        public async Task Listen(string playerId)
        {
            await Leave();

            var entity = await PlayerStore.Load(playerId);

            if (entity?.UserId != Context.UserIdentifier)
                throw new ActionForbidden();

            var player = Mapper.Map<TeamPlayer>(entity);

            await Groups.AddToGroupAsync(Context.ConnectionId, player.TeamId);

            Context.Items.Add(ContextKey, player);

            await Clients.OthersInGroup(player.TeamId).PresenceEvent(
                new HubEvent<TeamPlayer>(player, EventAction.Arrived)
            );
        }

        public Task Leave()
        {
            var player = Context.Items[ContextKey] as TeamPlayer;

            if (player is null)
                return Task.CompletedTask;

            Logger.LogDebug($"Leave {player.TeamId} {Context.User?.Identity.Name} {Context.ConnectionId}");

            Groups.RemoveFromGroupAsync(Context.ConnectionId, player.TeamId);

            Context.Items.Remove(ContextKey);

            return Clients.OthersInGroup(player.TeamId).PresenceEvent(
                new HubEvent<TeamPlayer>(player, EventAction.Departed)
            );
        }

        public Task Greet()
        {
            var player = Context.Items[ContextKey] as TeamPlayer;

            if (player is null)
                return Task.CompletedTask;

            return Clients.OthersInGroup(player.TeamId).PresenceEvent(
                new HubEvent<TeamPlayer>(player, EventAction.Greeted)
            );
        }
    }
}
