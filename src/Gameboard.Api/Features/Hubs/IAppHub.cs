// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Threading.Tasks;
using Gameboard.Api.Features.Games;

namespace Gameboard.Api.Hubs
{
    public interface IAppHubEvent
    {
        Task Announcement(HubEvent<Announcement> ev);
        Task PlayerEvent(HubEvent<TeamPlayer> ev);
        Task GameHubEvent(GameHubEvent<SyncStartState> ev);
        Task TeamEvent(HubEvent<TeamState> ev);
        Task ChallengeEvent(HubEvent<Challenge> challenge);
        Task TicketEvent(HubEvent<TicketNotification> ev);
    }

    public interface IAppHubAction
    {
        Task Listen(string id);
        Task Leave();
        Task<SyncStartState> JoinGame(string gameId);
        Task LeaveGame(string gameId);
    }
}
