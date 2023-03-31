// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using AutoMapper;
using Gameboard.Api.Data.Abstractions;
using Gameboard.Api.Features.Games;
using Gameboard.Api.Services;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Gameboard.Api.Hubs
{
    [Authorize(AppConstants.HubPolicy)]
    public class AppHub : Hub<IAppHubEvent>, IAppHubAction
    {
        ILogger Logger { get; }
        IPlayerStore PlayerStore { get; }
        internal static string ContextPlayerKey = "player";

        private readonly IGameService _gameService;
        private readonly IMapper _mapper;
        private readonly IMediator _mediator;

        public AppHub(
            ILogger<AppHub> logger,
            IMapper mapper,
            IGameService gameService,
            IMediator mediator,
            IPlayerStore playerStore
        )
        {
            Logger = logger;
            PlayerStore = playerStore;
            _gameService = gameService;
            _mapper = mapper;
            _mediator = mediator;
        }

        public override Task OnConnectedAsync()
        {
            Logger.LogDebug($"Session Connected: {Context.User.FindFirstValue("name")} {Context.UserIdentifier} {Context.ConnectionId}");
            return base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception ex)
        {
            Logger.LogDebug($"Session Disconnected: {Context.ConnectionId}");

            await Leave();
            await base.OnDisconnectedAsync(ex);
        }

        public async Task Listen(string teamId)
        {
            if (string.IsNullOrEmpty(teamId))
                return;

            if (Context.User.IsInRole(UserRole.Support.ToString()))
                await Groups.AddToGroupAsync(Context.ConnectionId, AppConstants.InternalSupportChannel);

            // ensure the player is on the right team
            var teamPlayers = await PlayerStore.ListTeam(teamId)
                .AsNoTracking()
                .ToArrayAsync();

            var player = teamPlayers.FirstOrDefault(p => p.UserId == Context.UserIdentifier);

            if (player == null)
                throw new PlayerIsntOnTeam();

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

        public async Task<SyncStartState> JoinGame(string gameId)
        {
            if (string.IsNullOrWhiteSpace(gameId))
                throw new ArgumentNullException();

            var game = await _gameService
                .BuildQuery()
                .AsNoTracking()
                .Include(g => g.Players)
                .FirstOrDefaultAsync(g => g.Id == gameId);

            if (game == null)
                throw new ResourceNotFound<Game>(gameId);

            if (!game.Players.Any(p => p.UserId == Context.UserIdentifier))
                throw new PlayerIsntInGame();

            await Groups.AddToGroupAsync(Context.ConnectionId, gameId);

            if (game.RequireSynchronizedStart)
            {
                return await _gameService.GetSyncStartState(game.Id);
            }

            // this isn't a failure, we just don't send anything down if sync start isn't needed
            return null;
        }

        public async Task LeaveGame(string gameId)
        {
            if (string.IsNullOrWhiteSpace(gameId))
                throw new ArgumentNullException();

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, gameId);
        }

        public async Task<Data.Player[]> ListTeam(string teamId)
        {
            var teamPlayers = await PlayerStore.DbSet
                .AsNoTrackingWithIdentityResolution()
                .Where(p => p.TeamId == teamId)
                .Include(p => p.Game)
                .Include(p => p.User)
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

        private HubEventActingUserDescription GetApiUser()
        {
            if (Context.Items.Keys.Contains(ContextPlayerKey))
            {
                var player = Context.Items[ContextPlayerKey] as Data.Player;
                return _mapper.Map<HubEventActingUserDescription>(player.User);
            }

            return null;
        }
    }
}
