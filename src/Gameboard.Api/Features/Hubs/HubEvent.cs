// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

namespace Gameboard.Api.Hubs;

public class HubEvent<T> where T : class
{
    public HubEventActingUserDescription ActingUser { get; set; }
    public EventAction Action { get; set; }
    public T Model { get; set; }

    public HubEvent(
        T model,
        EventAction action,
        HubEventActingUserDescription actingUser
    )
    {
        Action = action;
        Model = model;
        ActingUser = actingUser;
    }
}

public class HubEventActingUserDescription
{
    public required string Id { get; set; }
    public required string Name { get; set; }

    public static HubEventActingUserDescription FromUser(User user)
    {
        return new HubEventActingUserDescription
        {
            Id = user.Id,
            Name = user.ApprovedName
        };
    }
}

public enum EventAction
{
    Arrived,
    Departed,
    Created,
    RoleChanged,
    Updated,
    Deleted,
    Started
}


