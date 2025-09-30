// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

namespace Gameboard.Api.Tests.Integration.Fixtures;

public class TeamBuilderResult
{
    public required string TeamId { get; set; }
    public required Data.Game Game { get; set; }
    public required Data.Player Manager { get; set; }
}
