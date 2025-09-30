// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using Gameboard.Api.Features.GameEngine;

namespace Gameboard.Api.Features.Challenges;

public sealed class GetChallengeProgressResponse
{
    public required SimpleEntity Spec { get; set; }
    public required SimpleEntity Team { get; set; }
    public required GameEngineChallengeProgressView Progress { get; set; }
}
