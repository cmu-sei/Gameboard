// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

namespace Gameboard.Api.Hubs;

public enum GameboardHubType
{
    Game,
    Score,
    Support,
    Team,
    User
}

public class GameboardHubUserConnection
{
    public required string ConnectionId { get; set; }
    public required string UserId { get; set; }
}
