// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

namespace Gameboard.Api.Features.Practice;

public record ListGamesResponse(ListGamesResponseGame[] Games);

public sealed class ListGamesResponseGame
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required int ChallengeCount { get; set; }
}
