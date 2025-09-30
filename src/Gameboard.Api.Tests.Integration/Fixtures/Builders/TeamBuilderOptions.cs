// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using Gameboard.Api.Common;

namespace Gameboard.Api.Tests.Integration.Fixtures;

public class TeamBuilderOptions
{
    public required Action<Data.Game> GameBuilder { get; set; }
    public TeamBuilderOptionsManager? Manager { get; set; }
    public required string Name { get; set; }
    public required int NumPlayers { get; set; }
    public required SimpleEntity? Challenge { get; set; }
    public required string TeamId { get; set; }
}

public sealed class TeamBuilderOptionsManager
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? UserId { get; set; }
}
