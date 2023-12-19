using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Teams;
using Gameboard.Api.Structure.MediatR;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Challenges;

public record SaveChallengePendingSubmissionCommand(string ChallengeId, ChallengeSubmissionAnswers Answers) : IRequest;

internal class SaveChallengePendingSubmissionHandler : IRequestHandler<SaveChallengePendingSubmissionCommand>
{
    private readonly IActingUserService _actingUserService;
    private readonly IChallengeSubmissionsService _challengeSubmissionsService;
    private readonly IJsonService _jsonService;
    private readonly IStore _store;
    private readonly IValidatorService<SaveChallengePendingSubmissionCommand> _validatorService;

    public SaveChallengePendingSubmissionHandler
    (
        IActingUserService actingUserService,
        IChallengeSubmissionsService challengeSubmissionsService,
        IJsonService jsonService,
        IStore store,
        IValidatorService<SaveChallengePendingSubmissionCommand> validatorService
    )
    {
        _actingUserService = actingUserService;
        _challengeSubmissionsService = challengeSubmissionsService;
        _jsonService = jsonService;
        _store = store;
        _validatorService = validatorService;
    }

    public async Task Handle(SaveChallengePendingSubmissionCommand request, CancellationToken cancellationToken)
    {
        // validate
        _validatorService.AddValidator(async (req, ctx) =>
        {
            var teamId = await _store
                .WithNoTracking<Data.Challenge>()
                .Where(c => c.Id == req.ChallengeId)
                .Select(c => c.TeamId)
                .SingleOrDefaultAsync(cancellationToken);

            if (teamId is null)
            {
                ctx.AddValidationException(new ResourceNotFound<Data.Challenge>(req.ChallengeId));
                return;
            }

            var actingUserId = _actingUserService.Get().Id;
            var isPlaying = await _store
                .WithNoTracking<Data.Player>()
                .Where(p => p.UserId == _actingUserService.Get().Id && p.TeamId == teamId)
                .AnyAsync(cancellationToken);

            if (!isPlaying)
                ctx.AddValidationException(new UserIsntOnTeam(actingUserId, teamId));
        });

        // update the business
        await _challengeSubmissionsService.LogPendingSubmission
        (
            request.ChallengeId,
            request.Answers.QuestionSetIndex,
            request.Answers.Answers,
            cancellationToken
        );
    }
}
