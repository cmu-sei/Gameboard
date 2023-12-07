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
    Task LogSubmission(string challengeId, IEnumerable<string> answers, CancellationToken cancellationToken);
    Task LogSubmission(string challengeId, int sectionIndex, IEnumerable<string> answers, CancellationToken cancellationToken);
    Task LogPendingSubmission(string challengeId, IEnumerable<string> answers, CancellationToken cancellationToken);
    Task LogPendingSubmission(string challengeId, int sectionIndex, IEnumerable<string> answers, CancellationToken cancellationToken);
}

internal class ChallengeSubmissionsService : IChallengeSubmissionsService
{
    private readonly IJsonService _jsonService;
    private readonly INowService _now;
    private readonly IStore _store;

    public ChallengeSubmissionsService
    (
        IJsonService jsonService,
        INowService now,
        IStore store
    )
    {
        _jsonService = jsonService;
        _now = now;
        _store = store;
    }

    public Task LogPendingSubmission(string challengeId, IEnumerable<string> answers, CancellationToken cancellationToken)
        => LogPendingSubmission(challengeId, 0, answers, cancellationToken);

    public async Task LogPendingSubmission(string challengeId, int sectionIndex, IEnumerable<string> answers, CancellationToken cancellationToken)
    {
        var answersEntity = new ChallengeSubmissionAnswers
        {
            QuestionSetIndex = sectionIndex,
            Answers = answers
        };

        await _store
            .WithNoTracking<Data.Challenge>()
            .Where(c => c.Id == challengeId)
            .ExecuteUpdateAsync(up => up.SetProperty(c => c.PendingSubmission, _jsonService.Serialize(answersEntity)), cancellationToken);
    }

    public Task LogSubmission(string challengeId, IEnumerable<string> answers, CancellationToken cancellationToken)
        => LogSubmission(challengeId, answers, cancellationToken);

    public async Task LogSubmission(string challengeId, int sectionIndex, IEnumerable<string> answers, CancellationToken cancellationToken)
    {
        var answersEntity = new ChallengeSubmissionAnswers
        {
            QuestionSetIndex = sectionIndex,
            Answers = answers
        };

        // commit the new answers
        await _store.Create(new ChallengeSubmission
        {
            Answers = _jsonService.Serialize(answersEntity),
            ChallengeId = challengeId,
            SubmittedOn = _now.Get()
        }, cancellationToken);

        // upon submission, pending answers are cleared
        await _store
            .WithNoTracking<Data.Challenge>()
            .Where(c => c.Id == challengeId)
            .ExecuteUpdateAsync(up => up.SetProperty(c => c.PendingSubmission, null as string), cancellationToken);
    }
}
