// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

namespace Gameboard.Api.Data;

public enum PlayerRole
{
    Member = 0,
    Manager = 1
}

public enum PlayerMode
{
    Competition,
    Practice
}

public enum GameRegistrationType
{
    None,
    Open,
    Domain
}

public enum ChallengeEventType
{
    Started = 0,
    GamespaceOn = 1,
    GamespaceOff = 2,
    Submission = 3,
    SubmissionRejectedGamespaceExpired = 4,
    SubmissionRejectedGameEnded = 5,
    Regraded = 6
}

public enum ChallengeResult
{
    None,
    Partial,
    Success
}

public enum ActivityType
{
    Comment,
    StatusChange,
    AssigneeChange
}

public enum GameEngineType
{
    TopoMojo,
    Crucible
}
