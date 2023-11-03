// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using Gameboard.Api.Common;

namespace Gameboard.Api.Hubs;

public class HubEvent<T> where T : class
{
    public required SimpleEntity ActingUser { get; set; }
    public required EventAction Action { get; set; }
    public required T Model { get; set; }
}

public enum EventAction
{
    Arrived,
    Created,
    Deleted,
    Departed,
    ReadyStateChanged,
    RoleChanged,
    SessionExtended,
    Started,
    Updated
}
