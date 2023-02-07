// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

namespace Gameboard.Api.Hubs
{
    public class HubEvent<T> where T : class
    {
        public HubEvent(
            T model,
            EventAction action,
            string actorUserId
        )
        {
            Action = action;
            Model = model;
            ActorUserId = actorUserId;
        }

        public string ActorUserId { get; set; }
        public EventAction Action { get; set; }
        public T Model { get; set; }
    }

    public enum EventAction
    {
        Arrived,
        Greeted,
        Departed,
        Created,
        Updated,
        Deleted,
        Started
    }

}
