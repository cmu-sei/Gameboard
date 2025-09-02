// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gameboard.Api.Data;

public class Challenge : IEntity
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string ExternalId { get; set; }
    public string TeamId { get; set; }
    public string Tag { get; set; }
    public string GraderKey { get; set; }
    public string State { get; set; }
    public int Points { get; set; }
    public double Score { get; set; }
    public PlayerMode PlayerMode { get; set; }
    public DateTimeOffset LastScoreTime { get; set; }
    public DateTimeOffset LastSyncTime { get; set; }
    public DateTimeOffset WhenCreated { get; set; }
    public DateTimeOffset StartTime { get; set; }
    public DateTimeOffset EndTime { get; set; }
    public bool HasDeployedGamespace { get; set; }
    public GameEngineType GameEngineType { get; set; }

    /// <summary>
    /// A JSON string (using the ChallengeQuestionAnswerSetData model) of
    /// submitted answers. See JSONEntities.cs for an explanation.
    /// </summary>
    public string PendingSubmission { get; set; }

    [NotMapped]
    public ChallengeResult Result => Score >= Points
        ? ChallengeResult.Success
        : Score > 0
            ? ChallengeResult.Partial
            : ChallengeResult.None;

    [NotMapped]
    public long Duration => StartTime.IsNotEmpty() && LastScoreTime.IsNotEmpty()
        ? (long)LastScoreTime.Subtract(StartTime).TotalMilliseconds
        : 0;

    // nav properties
    public string GameId { get; set; }
    public Game Game { get; set; }
    public string PlayerId { get; set; }
    public Player Player { get; set; }
    public required string SpecId { get; set; }
    public Data.ChallengeSpec Spec { get; set; }

    // Control delete behavior with relationships
    public ICollection<AwardedChallengeBonus> AwardedBonuses { get; set; } = [];
    public ICollection<ManualChallengeBonus> AwardedManualBonuses { get; set; } = [];
    public ICollection<ChallengeEvent> Events { get; set; } = [];
    public ICollection<Feedback> Feedback { get; set; } = [];
    public ICollection<ChallengeSubmission> Submissions { get; set; } = [];
    public ICollection<Ticket> Tickets { get; set; } = [];
}
