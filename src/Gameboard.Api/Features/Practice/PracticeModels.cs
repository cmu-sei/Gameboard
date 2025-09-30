// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;

namespace Gameboard.Api.Features.Practice;

public sealed class AutoExtendPracticeSessionResult
{
    public required bool IsExtended { get; set; }
    public required DateTimeOffset SessionEnd { get; set; }
}

public sealed class ChallengeGroupsListArgs
{
    public string ContainChallengeSpecId { get; set; }
    public bool GetRootOnly { get; set; }
    public string GroupId { get; set; }
    public string ParentGroupId { get; set; }
    public string SearchTerm { get; set; }
}

public sealed class PracticeChallengeGroupDto
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required string Description { get; set; }
    public required string ImageUrl { get; set; }
    public required bool IsFeatured { get; set; }
    public required int ChallengeCount { get; set; }
    public required int ChallengeMaxScoreTotal { get; set; }
    public required PracticeChallengeGroupDtoChallenge[] Challenges { get; set; }
    public required string[] Tags { get; set; }
    public required SimpleEntity ParentGroup { get; set; }
    public required SimpleEntity[] ChildGroups { get; set; }
}

public sealed class PracticeChallengeGroupDtoChallenge
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required SimpleEntity Game { get; set; }
    public required string Description { get; set; }
    public required string[] Tags { get; set; }
    public required double MaxPossibleScore { get; set; }
    public required PracticeChallengeGroupDtoChallengeLaunchData LaunchData { get; set; }
}

public sealed class PracticeChallengeGroupDtoChallengeLaunchData
{
    public required int CountLaunches { get; set; }
    public required int CountCompletions { get; set; }
    public required DateTimeOffset? LastLaunch { get; set; }
}

public sealed class PracticeSession
{
    public required string GameId { get; set; }
    // would really love not to need PlayerId right now, but starting challenges with a teamId is
    // a big effort, as it happens
    public required string PlayerId { get; set; }
    public required TimestampRange Session { get; set; }
    public required string TeamId { get; set; }
    public required string UserId { get; set; }
}

public sealed class PracticeModeSettingsApiModel
{
    public required string Id { get; set; }
    public int? AttemptLimit { get; set; }
    public string CertificateTemplateId { get; set; }
    public required int DefaultPracticeSessionLengthMinutes { get; set; }
    public required string IntroTextMarkdown { get; set; }
    public int? MaxConcurrentPracticeSessions { get; set; }
    public int? MaxPracticeSessionLengthMinutes { get; set; }
    public required IEnumerable<string> SuggestedSearches { get; set; }
}

public sealed class UserPracticeHistoryChallenge
{
    public required string ChallengeId { get; set; }
    public required string ChallengeName { get; set; }
    public required string ChallengeSpecId { get; set; }
    public required int AttemptCount { get; set; }
    public required DateTimeOffset? BestAttemptDate { get; set; }
    public required double? BestAttemptScore { get; set; }
    public required bool IsComplete { get; set; }
}
