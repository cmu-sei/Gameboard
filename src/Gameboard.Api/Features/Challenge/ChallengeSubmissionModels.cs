// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using Gameboard.Api.Data;

namespace Gameboard.Api.Features.Challenges;

public sealed class ChallengeSubmissionCsvRecord
{
    public required string ChallengeSpecId { get; set; }
    public required string ChallengeSpecName { get; set; }
    public required string ChallengeId { get; set; }
    public required double ScoreAtSubmission { get; set; }
    public required double ScoreFinal { get; set; }
    public required double ScoreMaxPossible { get; set; }
    public required ChallengeSubmissionAnswers SubmittedAnswers { get; set; }
    public required DateTimeOffset SubmittedOn { get; set; }
    public required string UserId { get; set; }
    public required string UserName { get; set; }
}
