// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

namespace Gameboard.Api.Features.Challenges;

public sealed class StartChallengeResponse
{
    public required Challenge Challenge { get; set; }
}
