using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Teams;
using Gameboard.Api.Services;
using Gameboard.Api.Structure.MediatR;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Feedback;

public record UpsertFeedbackSubmissionCommand(UpsertFeedbackSubmissionRequest Request) : IRequest<UpsertFeedbackSubmissionResponse>;

// TODO: refactor this into separate commands for game/challenge that share an injected validator
internal sealed class UpsertFeedbackSubmissionHandler
(
    IActingUserService actingUserService,
    FeedbackService feedbackService,
    INowService now,
    IStore store,
    ITeamService teamService,
    IValidatorService validatorService
) : IRequestHandler<UpsertFeedbackSubmissionCommand, UpsertFeedbackSubmissionResponse>
{
    private readonly IActingUserService _actingUserService = actingUserService;
    private readonly FeedbackService _feedbackService = feedbackService;
    private readonly INowService _nowService = now;
    private readonly IStore _store = store;
    private readonly ITeamService _teamService = teamService;
    private readonly IValidatorService _validator = validatorService;

    public async Task<UpsertFeedbackSubmissionResponse> Handle(UpsertFeedbackSubmissionCommand request, CancellationToken cancellationToken)
    {
        var actingUserId = _actingUserService.Get()?.Id;

        _validator
            .Auth(c => c.RequireAuthentication())
            .AddValidator(ctx =>
            {
                if (request.Request.AttachedEntity.EntityType != FeedbackSubmissionAttachedEntityType.ChallengeSpec && request.Request.AttachedEntity.EntityType != FeedbackSubmissionAttachedEntityType.Game)
                {
                    ctx.AddValidationException(new InvalidParameterValue<FeedbackSubmissionAttachedEntityType>(nameof(request.Request.AttachedEntity.EntityType), "Must be either game or challengespec", request.Request.AttachedEntity.EntityType));
                }
            })
            .AddValidator(async ctx =>
            {
                if (!await _teamService.IsOnTeam(request.Request.AttachedEntity.TeamId, _actingUserService.Get().Id))
                {
                    ctx.AddValidationException(new UserIsntOnTeam(actingUserId, request.Request.AttachedEntity.TeamId, $"User isn't on the expected team."));
                }
            })
            .AddValidator(async ctx =>
            {
                var template = await _feedbackService.ResolveTemplate(request.Request.AttachedEntity.EntityType, request.Request.AttachedEntity.Id, cancellationToken);

                if (template is null || template.Id != request.Request.FeedbackTemplateId)
                {
                    ctx.AddValidationException(new InvalidFeedbackTemplateId(request.Request.FeedbackTemplateId, request.Request.AttachedEntity.EntityType, request.Request.AttachedEntity.Id));
                }

            })
            .AddEntityExistsValidator<FeedbackTemplate>(request.Request.FeedbackTemplateId);

        if (request.Request.AttachedEntity.EntityType == FeedbackSubmissionAttachedEntityType.ChallengeSpec)
        {
            _validator.AddEntityExistsValidator<Data.ChallengeSpec>(request.Request.AttachedEntity.Id);

            if (request.Request.Id.IsNotEmpty())
            {
                _validator.AddEntityExistsValidator<FeedbackSubmissionChallengeSpec>(request.Request.Id);
            }
        }
        else
        {
            _validator.AddEntityExistsValidator<Data.Game>(request.Request.AttachedEntity.Id);

            if (request.Request.Id.IsNotEmpty())
            {
                _validator.AddEntityExistsValidator<FeedbackSubmissionGame>(request.Request.Id);
            }
        }

        await _validator.Validate(cancellationToken);

        // if updating, update
        if (request.Request.Id.IsNotEmpty())
        {
            if (request.Request.AttachedEntity.EntityType == FeedbackSubmissionAttachedEntityType.ChallengeSpec)
            {
                var existingSubmission = await _store
                    .WithNoTracking<FeedbackSubmissionChallengeSpec>()
                    .Where(s => s.Id == request.Request.Id)
                    .SingleAsync(cancellationToken);

                existingSubmission.WhenEdited = _nowService.Get();
                existingSubmission.Responses = [.. request.Request.Responses];
                await _store.SaveUpdate(existingSubmission, cancellationToken);
                return new UpsertFeedbackSubmissionResponse { Submission = existingSubmission };
            }
            else if (request.Request.AttachedEntity.EntityType == FeedbackSubmissionAttachedEntityType.Game)
            {
                var existingSubmission = await _store
                    .WithNoTracking<FeedbackSubmissionGame>()
                    .Where(s => s.Id == request.Request.Id)
                    .SingleAsync(cancellationToken);

                existingSubmission.WhenEdited = _nowService.Get();
                existingSubmission.Responses = [.. request.Request.Responses];
                await _store.SaveUpdate(existingSubmission, cancellationToken);
                return new UpsertFeedbackSubmissionResponse { Submission = existingSubmission };
            }
        }
        else
        {
            // if creating, create
            if (request.Request.AttachedEntity.EntityType == FeedbackSubmissionAttachedEntityType.ChallengeSpec)
            {
                var result = await _store
                    .Create<FeedbackSubmissionChallengeSpec>(new()
                    {
                        ChallengeSpecId = request.Request.AttachedEntity.Id,
                        FeedbackTemplateId = request.Request.FeedbackTemplateId,
                        Responses = [.. request.Request.Responses],
                        TeamId = request.Request.AttachedEntity.TeamId,
                        UserId = actingUserId,
                        WhenSubmitted = _nowService.Get(),
                    }, cancellationToken);

                return new UpsertFeedbackSubmissionResponse { Submission = result };
            }
            else
            {
                var result = await _store
                    .Create<FeedbackSubmissionGame>(new()
                    {
                        GameId = request.Request.AttachedEntity.Id,
                        FeedbackTemplateId = request.Request.FeedbackTemplateId,
                        Responses = [.. request.Request.Responses],
                        TeamId = request.Request.AttachedEntity.TeamId,
                        UserId = actingUserId,
                        WhenSubmitted = _nowService.Get(),
                    }, cancellationToken);

                return new UpsertFeedbackSubmissionResponse { Submission = result };
            }
        }

        throw new NotImplementedException();
    }
}
