// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

namespace Gameboard.Api.Features.Practice;

public sealed class ListChallengeTagsResponse
{
    public required ListChallengeTagsResponseTag[] ChallengeTags { get; set; }
}

public sealed class ListChallengeTagsResponseTag
{
    public required string Tag { get; set; }
    public required int ChallengeCount { get; set; }
}
