// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;

namespace Gameboard.Api.Features.Practice;

public sealed class GetPracticeChallengeGroupResponse
{
    public required PracticeChallengeGroupDto Group { get; set; }
}

// public sealed class GetPracticeChallengeGroupResponseGroup
// {
//     public required string Id { get; set; }
//     public required string Name { get; set; }
//     public required string Description { get; set; }
//     public required string ImageUrl { get; set; }
//     public required bool IsFeatured { get; set; }
//     public required GetPracticeChallengeGroupResponseChallenge[] Challenges { get; set; }
// }

// public sealed class GetPracticeChallengeGroupResponseChallenge
// {
//     public required string Id { get; set; }
//     public required string Name { get; set; }
//     public required SimpleEntity Game { get; set; }
//     public required int CountCompleted { get; set; }
//     public required int CountLaunched { get; set; }
//     public required DateTimeOffset? LastLaunched { get; set; }
// }
