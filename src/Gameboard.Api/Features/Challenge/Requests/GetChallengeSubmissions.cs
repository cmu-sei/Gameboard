using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Data;
using Gameboard.Api.Structure.MediatR;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Challenges;

public sealed class GetChallengeSubmissionsResponse
{
    public required string ChallengeId { get; set; }
    public required string TeamId { get; set; }
    public ChallengeSubmissionAnswers PendingAnswers { get; set; }
    public IEnumerable<ChallengeSubmissionViewModel> SubmittedAnswers { get; set; }
}

public record GetChallengeSubmissionsQuery(string ChallengeId) : IRequest<GetChallengeSubmissionsResponse>;

internal class GetChallengeSubmissionsHandler : IRequestHandler<GetChallengeSubmissionsQuery, GetChallengeSubmissionsResponse>
{
    private readonly IActingUserService _actingUserService;
    private readonly IJsonService _jsonService;
    private readonly IStore _store;
    private readonly IGameboardRequestValidator<GetChallengeSubmissionsQuery> _validatorService;

    public GetChallengeSubmissionsHandler
    (
        IActingUserService actingUserService,
        IJsonService jsonService,
        IStore store,
        IGameboardRequestValidator<GetChallengeSubmissionsQuery> validatorService
    )
    {
        _actingUserService = actingUserService;
        _jsonService = jsonService;
        _store = store;
        _validatorService = validatorService;
    }

    public async Task<GetChallengeSubmissionsResponse> Handle(GetChallengeSubmissionsQuery request, CancellationToken cancellationToken)
    {
        // lots to validate, so we do it in a custom validator
        await _validatorService.Validate(request, cancellationToken);

        // get pending and past submissions 
        // both are strings that are secretly JSON, for Reasons (see JsonEntities.cs)
        // The Challenges table has up to one instance per record, and the 
        // ChallengeSubmissions table has exactly one per record. Each represents
        // a section index (corresponding to Topomojo's Question Sets, and nearly
        // always 0) and an array of string answers.
        var rawSubmissionData = await _store
            .WithNoTracking<Data.Challenge>()
            .Include(c => c.Submissions.OrderBy(s => s.SubmittedOn))
            .Select(c => new
            {
                ChallengeId = c.Id,
                c.TeamId,
                PendingSubmissionData = c.PendingSubmission,
                c.Submissions
            })
            .SingleAsync(c => c.ChallengeId == request.ChallengeId, cancellationToken);

        ChallengeSubmissionAnswers pendingAnswers = null;
        var submittedAnswers = Array.Empty<ChallengeSubmissionViewModel>();

        if (rawSubmissionData.PendingSubmissionData.IsNotEmpty())
        {
            pendingAnswers = _jsonService.Deserialize<ChallengeSubmissionAnswers>(rawSubmissionData.PendingSubmissionData);
        }

        if (rawSubmissionData.Submissions is not null)
        {
            submittedAnswers = rawSubmissionData.Submissions.Select
            (
                s =>
                {
                    var asModel = _jsonService.Deserialize<ChallengeSubmissionAnswers>(s.Answers);

                    return new ChallengeSubmissionViewModel
                    {
                        SubmittedOn = s.SubmittedOn,
                        SectionIndex = asModel.QuestionSetIndex,
                        Answers = asModel.Answers
                    };
                }
            ).ToArray();
        }

        return new GetChallengeSubmissionsResponse
        {
            ChallengeId = request.ChallengeId,
            TeamId = rawSubmissionData.TeamId,
            PendingAnswers = pendingAnswers,
            SubmittedAnswers = submittedAnswers
        };
    }
}
