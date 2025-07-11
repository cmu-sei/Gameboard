// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using Gameboard.Api.Data;
using Gameboard.Api.Features.GameEngine;

namespace Gameboard.Api;

public class Challenge
{
    public string Id { get; set; }
    public string SpecId { get; set; }
    public string TeamId { get; set; }
    public string Name { get; set; }
    public string Tag { get; set; }
    public string FeedbackTemplateId { get; set; }
    public DateTimeOffset StartTime { get; set; }
    public DateTimeOffset EndTime { get; set; }
    public DateTimeOffset LastScoreTime { get; set; }
    public DateTimeOffset LastSyncTime { get; set; }
    public bool HasDeployedGamespace { get; set; }
    public int Points { get; set; }
    public int Score { get; set; }
    public long Duration { get; set; }
    public ChallengeResult Result { get; set; }
    public ChallengeEventSummary[] Events { get; set; }
    public GameEngineType GameEngineType { get; set; }
    public GameEngineGameState State { get; set; }
}

public class ChallengeSummary
{
    public string Id { get; set; }
    public string TeamId { get; set; }
    public string Name { get; set; }
    public string Tag { get; set; }
    public string GameName { get; set; }
    public IEnumerable<ChallengePlayer> Players { get; set; }
    public DateTimeOffset StartTime { get; set; }
    public DateTimeOffset EndTime { get; set; }
    public DateTimeOffset LastScoreTime { get; set; }
    public DateTimeOffset LastSyncTime { get; set; }
    public bool HasDeployedGamespace { get; set; }
    public int Points { get; set; }
    public int Score { get; set; }
    public long Duration { get; set; }
    public ChallengeResult Result { get; set; }
    public ChallengeEventSummary[] Events { get; set; }
    public bool IsActive { get; set; }
}

public sealed class ChallengeLaunchCacheEntry
{
    public required string TeamId { get; set; }
    public required IList<ChallengeLaunchCacheEntrySpec> Specs { get; set; } = [];
}

public sealed class ChallengeLaunchCacheEntrySpec
{
    public required string GameId { get; set; }
    public required string SpecId { get; set; }
}

public class UserActiveChallenge
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required long? EndsAt { get; set; }
    public required SimpleEntity Game { get; set; }
    public required SimpleEntity Spec { get; set; }
    public required string FeedbackTemplateId { get; set; }
    public required bool IsDeployed { get; set; }
    public required string Markdown { get; set; }
    public required PlayerMode Mode { get; set; }
    public required SimpleEntity Team { get; set; }
    public required UserActiveChallengeScoreAndAttemptsState ScoreAndAttemptsState { get; set; }
    public required IEnumerable<UserActiveChallengeVm> Vms { get; set; }
}

public class UserActiveChallengeVm
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required string AccessTicket { get; set; }
    public required string Url { get; set; }
}

public class UserActiveChallengeScoreAndAttemptsState
{
    public required int Attempts { get; set; }
    public required int? MaxAttempts { get; set; }
    public required decimal Score { get; set; }
    public required decimal MaxPossibleScore { get; set; }
}

public class ChallengePlayer
{
    public string Id { get; set; }
    public string Name { get; set; }
    public bool IsManager { get; set; }
    public string UserId { get; set; }
}

public class NewChallenge
{
    public required string SpecId { get; set; }
    public required string PlayerId { get; set; }
    public bool StartGamespace { get; set; } = true;
    public int Variant { get; set; }
}

public class ChangedChallenge
{
    public string Id { get; set; }
}

public class TeamChallenge
{
    public string Id { get; set; }
    public string TeamId { get; set; }
    public string Name { get; set; }
    public string Tag { get; set; }
    public int Points { get; set; }
    public double Score { get; set; }
    public long Duration { get; set; }
    public ChallengeResult Result { get; set; }
    public ChallengeEventSummary[] Events { get; set; }
}

public class ChallengeOverview
{
    public string Id { get; set; }
    public string TeamId { get; set; }
    public string GameId { get; set; }
    public string GameName { get; set; }
    public string Name { get; set; }
    public string Tag { get; set; }
    public int Points { get; set; }
    public double Score { get; set; }
    public long Duration { get; set; }
    public bool AllowTeam { get; set; }
}

public sealed class ChallengeSolutionGuide
{
    public required string ChallengeSpecId { get; set; }
    public required bool ShowInCompetitiveMode { get; set; }
    public required string Url { get; set; }
}

public class ObserveChallenge
{
    public string Id { get; set; }
    public string TeamId { get; set; }
    public string TeamName { get; set; }
    public string Name { get; set; }
    public string Tag { get; set; }
    public string PlayerId { get; set; }
    public string PlayerName { get; set; }
    public long Duration { get; set; }
    public int ChallengeScore { get; set; }
    public int GameScore { get; set; }
    public int GameRank { get; set; }
    public bool IsActive { get; set; }
    public ObserveVM[] Consoles { get; set; }
}

public class ObserveVM
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string ChallengeId { get; set; }
    public bool IsRunning { get; set; }
    public bool IsVisible { get; set; }
}

public class GetConsoleStateRequest
{
    public string ChallengeId { get; set; }
    public string Name { get; set; }
}

public class ConsoleRequest
{
    public string Name { get; set; }
    public string SessionId { get; set; }
    public ConsoleAction Action { get; set; }
    public string Id { get => $"{Name}#{SessionId}"; }
}

public enum ConsoleAction
{
    None,
    Ticket,
    Reset
}

public class ConsoleActor
{
    public string UserId { get; set; }
    public string UserName { get; set; }
    public string PlayerName { get; set; }
    public string ChallengeName { get; set; }
    public string ChallengeId { get; set; }
    public string GameId { get; set; }
    public string TeamId { get; set; }
    public string VmName { get; set; }
    public DateTimeOffset Timestamp { get; set; }
}

public class ArchivedChallenge
{
    public string Id { get; set; }
    public string TeamId { get; set; }
    public string Name { get; set; }
    public string Tag { get; set; }
    public string GameId { get; set; }
    public string GameName { get; set; }
    public string PlayerId { get; set; }
    public string PlayerName { get; set; }
    public string UserId { get; set; }
    public DateTimeOffset StartTime { get; set; }
    public DateTimeOffset EndTime { get; set; }
    public DateTimeOffset LastScoreTime { get; set; }
    public DateTimeOffset LastSyncTime { get; set; }
    public bool HasGamespaceDeployed { get; set; }
    public int Points { get; set; }
    public int Score { get; set; }
    public long Duration { get; set; }
    public ChallengeResult Result { get; set; }
    public ChallengeEventSummary[] Events { get; set; }
    public string[] TeamMembers { get; set; } // User Ids of all team members
    public bool IsActive { get; set; }
    public GameEngineSectionSubmission[] Submissions { get; set; }
}

public class ChallengeEventSummary
{
    public string UserId { get; set; }
    public string Text { get; set; }
    public ChallengeEventType Type { get; set; }
    public DateTimeOffset Timestamp { get; set; }
}

public class ChallengeSearchFilter : SearchFilter
{
    public string uid { get; set; } // Used to search for all challenges of a user
}

public sealed class ChallengeIdUserIdMap
{
    public required IDictionary<string, string[]> ChallengeIdUserIds { get; set; }
    public required IDictionary<string, string[]> UserIdChallengeIds { get; set; }
}
