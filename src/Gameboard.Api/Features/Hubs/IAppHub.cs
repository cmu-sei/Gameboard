// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Threading.Tasks;

namespace Gameboard.Api.Hubs
{
    public interface IAppHubEvent
    {
        Task Announcement(HubEvent<Announcement> ev);
        Task PresenceEvent(HubEvent<TeamPlayer> ev);
        Task TeamEvent(HubEvent<TeamState> ev);
        Task ChallengeEvent(HubEvent<Challenge> challenge);
    }

    public interface IAppHubAction
    {
        Task Listen(string id);
        Task Leave();
        Task Greet();
    }

}
