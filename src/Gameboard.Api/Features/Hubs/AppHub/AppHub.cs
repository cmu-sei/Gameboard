// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using AutoMapper;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Teams;
using Gameboard.Api.Features.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Gameboard.Api.Hubs
{
    [Authorize(AppConstants.HubPolicy)]
    public class AppHub(
        ILogger<AppHub> logger,
        IMapper mapper,
        IUserRolePermissionsService permissionsService,
        IPlayerStore playerStore,
        IStore store
        ) : Hub<IAppHubEvent>, IAppHubApi
    {
        ILogger Logger { get; } = logger;
        IPlayerStore PlayerStore { get; } = playerStore;
        internal static string ContextPlayerKey = "player";

        private readonly IMapper _mapper = mapper;
        private readonly IStore _store = store;
        private readonly IUserRolePermissionsService _permissionsService = permissionsService;

        public override Task OnConnectedAsync()
        {
            Logger.LogDebug(message: $"Session Connected: {Context.User.FindFirstValue("name")} {Context.UserIdentifier} {Context.ConnectionId}");
            return base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception ex)
        {
            Logger.LogDebug(message: $"Session Disconnected: {Context.ConnectionId}");

            await Leave();
            await base.OnDisconnectedAsync(ex);
        }

        public async Task Listen(string teamId)
        {
            if (string.IsNullOrEmpty(teamId))
                return;

            if (await _permissionsService.Can(PermissionKey.Support_ManageTickets))
                await Groups.AddToGroupAsync(Context.ConnectionId, AppConstants.InternalSupportChannel);

            // ensure the player is on the right team
            var player = await _store
                .WithNoTracking<Data.Player>()
                .Where(p => p.UserId == Context.UserIdentifier)
                .Where(p => p.TeamId == teamId)
                .SingleOrDefaultAsync() ?? throw new UserIsntOnTeam(Context.UserIdentifier, teamId);

            if (Context.Items[ContextPlayerKey] != null)
                Context.Items.Remove(ContextPlayerKey);

            Context.Items.Add(ContextPlayerKey, player);

            // project, add to group, and broadcast
            var teamPlayer = _mapper.Map<TeamPlayer>(player);
            await Groups.AddToGroupAsync(Context.ConnectionId, player.TeamId);
            await Clients.OthersInGroup(player.TeamId).PlayerEvent(
                new HubEvent<TeamPlayer>
                {
                    Model = teamPlayer,
                    Action = EventAction.Arrived,
                    ActingUser = GetApiUser()
                }
            );
        }

        public async Task LeaveChannel(string channelId)
        {
            if (string.IsNullOrWhiteSpace(channelId))
                throw new ArgumentNullException(nameof(channelId));

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, channelId);
        }

        public async Task<Data.Player[]> ListTeam(string teamId)
        {
            var teamPlayers = await PlayerStore.DbSet
                .AsNoTrackingWithIdentityResolution()
                .Where(p => p.TeamId == teamId)
                .Include(p => p.Game)
                .Include(p => p.User)
                .Include(p => p.Sponsor)
                .ToArrayAsync();

            return teamPlayers;
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
                    Clients.OthersInGroup(player.TeamId).PlayerEvent(
                        new HubEvent<TeamPlayer>
                        {
                            Model = player,
                            Action = EventAction.Departed,
                            ActingUser = GetApiUser()
                        }
                    )
                };

                Context.Items.Remove(ContextPlayerKey);
            }

            return Task.WhenAll(tasks);
        }

        private SimpleEntity GetApiUser()
        {
            if (Context.Items.ContainsKey(ContextPlayerKey))
            {
                var player = Context.Items[ContextPlayerKey] as Data.Player;
                return new SimpleEntity { Id = player.UserId, Name = player.ApprovedName };
            }

            return null;
        }
    }
}
