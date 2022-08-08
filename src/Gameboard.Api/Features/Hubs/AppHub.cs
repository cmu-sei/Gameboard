// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
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
        const string ContextPlayerKey = "player";

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

        public async Task Listen(string id)
        {
            await Leave();

            if (Context.User.IsInRole(UserRole.Support.ToString()))
                await Groups.AddToGroupAsync(Context.ConnectionId, AppConstants.InternalSupportChannel);
            
            if (id == Context.UserIdentifier)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, id);
            }
            else
            {
                var entity = await PlayerStore.Load(id);

                if (entity?.UserId == Context.UserIdentifier)
                {
                    var player = Mapper.Map<TeamPlayer>(entity);

                    Context.Items.Add(ContextPlayerKey, player);

                    await Groups.AddToGroupAsync(Context.ConnectionId, player.TeamId);

                    await Clients.OthersInGroup(player.TeamId).PresenceEvent(
                        new HubEvent<TeamPlayer>(player, EventAction.Arrived)
                    );
                }
            }

        }

        public Task Leave()
        {
            Task[] tasks;

            var player = Context.Items[ContextPlayerKey] as TeamPlayer;

            if (player is null)
            {
                tasks = new Task[] {
                    Groups.RemoveFromGroupAsync(Context.ConnectionId, Context.UserIdentifier),
                    Groups.RemoveFromGroupAsync(Context.ConnectionId, AppConstants.InternalSupportChannel)
                };
            }
            else
            {

                Logger.LogDebug($"Leave {player.TeamId} {Context.User?.Identity.Name} {Context.ConnectionId}");
                
                tasks = new Task[] {
                    Groups.RemoveFromGroupAsync(Context.ConnectionId, player.TeamId),
                    Groups.RemoveFromGroupAsync(Context.ConnectionId, AppConstants.InternalSupportChannel),
                    Clients.OthersInGroup(player.TeamId).PresenceEvent(
                        new HubEvent<TeamPlayer>(player, EventAction.Departed)
                    )
                };

                Context.Items.Remove(ContextPlayerKey);
            }

            return Task.WhenAll(tasks);
        }

        public Task Greet()
        {
            var player = Context.Items[ContextPlayerKey] as TeamPlayer;

            if (player is null)
                return Task.CompletedTask;

            return Clients.OthersInGroup(player.TeamId).PresenceEvent(
                new HubEvent<TeamPlayer>(player, EventAction.Greeted)
            );
        }
    }
}
