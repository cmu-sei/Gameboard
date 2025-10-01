// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using Gameboard.Api.Features.Games.External;

namespace Gameboard.Api.Data;

/// <summary>
/// Holds metadata about each team which participates in a game
/// with Engine Mode set to "External". 
/// 
/// The two primary pieces of useful info are the game deploy status
/// (which tells clients where to send users depending on the deploy
/// status of the game) and the external URL, which we currently
/// assume to point to a team-specific service (like a headless URL
/// for a Unity game)
/// </summary>
public class ExternalGameTeam : IEntity
{
    public string Id { get; set; }
    public string TeamId { get; set; }
    public string ExternalGameUrl { get; set; }
    public ExternalGameDeployStatus DeployStatus { get; set; }

    // nav properties
    public string GameId { get; set; }
    public Game Game { get; set; }
}
