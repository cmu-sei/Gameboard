// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Collections.Generic;
using Gameboard.Api.Features.Scores;

namespace Gameboard.Api.Features.Admin;

public sealed class TeamCenterContext
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required Score Score { get; set; }
    public required SimpleEntity Captain { get; set; }
    public required IEnumerable<TeamCenterContextPlayer> Players { get; set; }
    public required IEnumerable<TeamCenterContextChallenge> Challenges { get; set; }
}

public sealed class TeamCenterContextChallenge
{
    public required string Id { get; set; }
    public required long? Start { get; set; }
    public required long? End { get; set; }
    public required Score Score { get; set; }
    public required SimpleEntity Spec { get; set; }
}

public sealed class TeamCenterContextPlayer
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required SimpleSponsor Sponsor { get; set; }
    public required SimpleEntity User { get; set; }
}
