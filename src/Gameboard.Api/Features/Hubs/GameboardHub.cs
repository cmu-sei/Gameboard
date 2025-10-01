// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using Microsoft.AspNetCore.SignalR;

namespace Gameboard.Api.Hubs;

public interface IGameboardHub : ICanonicalGroupIdProvider
{
    public HubCallerContext Context { get; }
    public IGroupManager Groups { get; }
}

public interface IGameboardHubService : ICanonicalGroupIdProvider { }
