using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Data;
using Gameboard.Api.Services;
using Gameboard.Api.Structure.MediatR;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Feedback;

public record UpsertFeedbackSubmissionCommand(UpsertFeedbackSubmissionRequest Request) : IRequest<FeedbackSubmissionView>;

// TODO: refactor this into separate commands for game/challenge that share an injected validator
internal sealed class UpsertFeedbackSubmissionHandler
(
    IActingUserService actingUserService,
    FeedbackService feedbackService,
    INowService now,
    IStore store,
    IValidatorService validatorService
) : IRequestHandler<UpsertFeedbackSubmissionCommand, FeedbackSubmissionView>
{
    private readonly IActingUserService _actingUserService = actingUserService;
    private readonly FeedbackService _feedbackService = feedbackService;
    private readonly INowService _nowService = now;
    private readonly IStore _store = store;
    private readonly IValidatorService _validator = validatorService;

    public async Task<FeedbackSubmissionView> Handle(UpsertFeedbackSubmissionCommand request, CancellationToken cancellationToken)
    {
        var actingUserId = _actingUserService.Get()?.Id;

        await _validator
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
                var template = await _feedbackService.ResolveTemplate(request.Request.AttachedEntity.EntityType, request.Request.AttachedEntity.Id, cancellationToken);

                if (template is null || template.Id != request.Request.FeedbackTemplateId)
                {
                    ctx.AddValidationException(new InvalidFeedbackTemplateId(request.Request.FeedbackTemplateId, request.Request.AttachedEntity.EntityType, request.Request.AttachedEntity.Id));
                }

            })
            .AddEntityExistsValidator<FeedbackTemplate>(request.Request.FeedbackTemplateId)
            .AddValidator(async ctx =>
            {
                var existingSubmission = await _feedbackService.ResolveExistingSubmission
                (
                    _actingUserService.Get().Id,
                    request.Request.AttachedEntity.EntityType,
                    request.Request.AttachedEntity.Id,
                    cancellationToken
                );

                if (existingSubmission?.WhenFinalized is not null)
                {
                    ctx.AddValidationException(new FeedbackSubmissionFinalized(existingSubmission.Id, request.Request.AttachedEntity.EntityType, existingSubmission.WhenFinalized.Value));
                }
            })
            .Validate(cancellationToken);

        // we don't have them update by id since the user id + entity are a unique key
        // so load any previous submission to check for update
        var existingSubmission = await _feedbackService.ResolveExistingSubmission
        (
            _actingUserService.Get().Id,
            request.Request.AttachedEntity.EntityType,
            request.Request.AttachedEntity.Id,
            cancellationToken
        );

        // ultimately our retval
        var submissionModel = default(FeedbackSubmission);

        // we also need the template so we can be sure to save an answer for every question, even if not supplied previously
        var template = await _store
            .WithNoTracking<FeedbackTemplate>()
            .Where(t => t.Id == request.Request.FeedbackTemplateId)
            .SingleAsync(cancellationToken);

        // if updating, update
        if (existingSubmission is not null)
        {
            if (request.Request.AttachedEntity.EntityType == FeedbackSubmissionAttachedEntityType.ChallengeSpec)
            {
                submissionModel = await _store
                    .WithNoTracking<FeedbackSubmissionChallengeSpec>()
                    .Where(s => s.Id == existingSubmission.Id)
                    .SingleAsync(cancellationToken);
            }
            else if (request.Request.AttachedEntity.EntityType == FeedbackSubmissionAttachedEntityType.Game)
            {
                submissionModel = await _store
                    .WithNoTracking<FeedbackSubmissionGame>()
                    .Where(s => s.Id == existingSubmission.Id)
                    .SingleAsync(cancellationToken);
            }

            submissionModel.WhenEdited = _nowService.Get();
            submissionModel.Responses.Clear();

            foreach (var question in _feedbackService.BuildQuestionConfigFromTemplate(template).Questions)
            {
                submissionModel.Responses.Add(new QuestionSubmission
                {
                    Id = question.Prompt,
                    Answer = submissionModel.Responses.SingleOrDefault(r => r.Id == question.Id)?.Answer,
                    Prompt = question.Prompt,
                    ShortName = question.ShortName,
                });
            }

            if (request.Request.IsFinalized && submissionModel.WhenFinalized is null)
            {
                submissionModel.WhenFinalized = _nowService.Get();
            }
            submissionModel = await _store.SaveUpdate(submissionModel, cancellationToken);
        }
        else
        {
            // if creating, create
            if (request.Request.AttachedEntity.EntityType == FeedbackSubmissionAttachedEntityType.ChallengeSpec)
            {
                submissionModel = await _store
                    .Create<FeedbackSubmissionChallengeSpec>(new()
                    {
                        ChallengeSpecId = request.Request.AttachedEntity.Id,
                        FeedbackTemplateId = request.Request.FeedbackTemplateId,
                        Responses = [.. request.Request.Responses],
                        UserId = actingUserId,
                        WhenCreated = _nowService.Get(),
                    }, cancellationToken);
            }
            else
            {
                submissionModel = await _store
                    .Create<FeedbackSubmissionGame>(new()
                    {
                        GameId = request.Request.AttachedEntity.Id,
                        FeedbackTemplateId = request.Request.FeedbackTemplateId,
                        Responses = [.. request.Request.Responses],
                        UserId = actingUserId,
                        WhenCreated = _nowService.Get(),
                    }, cancellationToken);
            }
        }

        if (submissionModel == default)
        {
            throw new NotImplementedException();
        }

        return await _feedbackService.ResolveExistingSubmission
        (
            _actingUserService.Get().Id,
            request.Request.AttachedEntity.EntityType,
            request.Request.AttachedEntity.Id,
            cancellationToken
        );
    }
}
