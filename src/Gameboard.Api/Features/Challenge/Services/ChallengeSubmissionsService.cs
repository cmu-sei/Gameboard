// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Challenges;

public interface IChallengeSubmissionsService
{
    Task<ChallengeSubmissionCsvRecord[]> GetSubmissionsCsv(IQueryable<Data.Challenge> challengesQuery, CancellationToken cancellationToken);
    Task LogSubmission(string challengeId, double score, string[] answers, CancellationToken cancellationToken);
    Task LogSubmission(string challengeId, double score, int sectionIndex, string[] answers, CancellationToken cancellationToken);
    Task LogPendingSubmission(string challengeId, string[] answers, CancellationToken cancellationToken);
    Task LogPendingSubmission(string challengeId, int sectionIndex, string[] answers, CancellationToken cancellationToken);
}

internal class ChallengeSubmissionsService
(
    IJsonService json,
    INowService now,
    IStore store
) : IChallengeSubmissionsService
{
    private readonly IJsonService _json = json;
    private readonly INowService _now = now;
    private readonly IStore _store = store;

    public async Task<ChallengeSubmissionCsvRecord[]> GetSubmissionsCsv(IQueryable<Data.Challenge> challengesQuery, CancellationToken cancellationToken)
    {
        var challengeData = challengesQuery.Select(c => new
        {
            c.Id,
            c.Name,
            c.Points,
            c.Player.UserId,
            c.Score,
            c.SpecId,
            UserName = c.Player.User.ApprovedName
        })
        .ToDictionary(c => c.Id, c => c);

        var submissions = await _store
            .WithNoTracking<ChallengeSubmission>()
            .Where(s => challengeData.Keys.Contains(s.ChallengeId))
            .ToArrayAsync(cancellationToken);

        var records = new List<ChallengeSubmissionCsvRecord>();
        foreach (var s in submissions)
        {
            var challenge = challengeData[s.ChallengeId];
            var deserializedAnswers = _json.Deserialize<ChallengeSubmissionAnswers>(s.Answers);

            records.Add(new()
            {
                ChallengeId = challenge.Id,
                ChallengeSpecId = challenge.SpecId,
                ChallengeSpecName = challenge.Name,
                ScoreAtSubmission = s.Score,
                ScoreFinal = challenge.Score,
                ScoreMaxPossible = challenge.Points,
                SubmittedAnswers = deserializedAnswers,
                SubmittedOn = s.SubmittedOn,
                UserId = challenge.UserId,
                UserName = challenge.UserName
            });
        }

        return [..
            records
                .OrderBy(r => r.ChallengeSpecName)
                .ThenBy(r => r.ChallengeId)
                .ThenBy(r => r.SubmittedAnswers.QuestionSetIndex)
        ];
    }

    public Task LogPendingSubmission(string challengeId, string[] answers, CancellationToken cancellationToken)
        => LogPendingSubmission(challengeId, 0, answers, cancellationToken);

    public async Task LogPendingSubmission(string challengeId, int sectionIndex, string[] answers, CancellationToken cancellationToken)
    {
        var answersEntity = new ChallengeSubmissionAnswers
        {
            QuestionSetIndex = sectionIndex,
            Answers = answers
        };

        await _store
            .WithNoTracking<Data.Challenge>()
            .Where(c => c.Id == challengeId)
            .ExecuteUpdateAsync(up => up.SetProperty(c => c.PendingSubmission, _json.Serialize(answersEntity)), cancellationToken);
    }

    public Task LogSubmission(string challengeId, double score, string[] answers, CancellationToken cancellationToken)
        => LogSubmission(challengeId, score, 0, answers, cancellationToken);

    public async Task LogSubmission(string challengeId, double score, int sectionIndex, string[] answers, CancellationToken cancellationToken)
    {
        var answersEntity = new ChallengeSubmissionAnswers
        {
            QuestionSetIndex = sectionIndex,
            Answers = answers
        };

        // commit the new answers
        await _store.Create(new ChallengeSubmission
        {
            Answers = _json.Serialize(answersEntity),
            ChallengeId = challengeId,
            Score = score,
            SubmittedOn = _now.Get()
        }, cancellationToken);

        // upon submission, pending answers are cleared
        var rowsAffected = await _store
            .WithNoTracking<Data.Challenge>()
            .Where(c => c.Id == challengeId)
            .ExecuteUpdateAsync(up => up.SetProperty(c => c.PendingSubmission, null as string), cancellationToken);
    }
}
