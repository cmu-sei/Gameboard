// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Users;
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
    IUserRolePermissionsService permissions,
    IStore store,
    IValidatorService validatorService
) : IRequestHandler<UpsertFeedbackSubmissionCommand, FeedbackSubmissionView>
{
    private readonly IActingUserService _actingUserService = actingUserService;
    private readonly FeedbackService _feedbackService = feedbackService;
    private readonly INowService _nowService = now;
    private readonly IUserRolePermissionsService _permissions = permissions;
    private readonly IStore _store = store;
    private readonly IValidatorService _validator = validatorService;

    public async Task<FeedbackSubmissionView> Handle(UpsertFeedbackSubmissionCommand request, CancellationToken cancellationToken)
    {
        await _validator
            .Auth(c => c.RequireAuthentication())
            .AddEntityExistsValidator<FeedbackTemplate>(request.Request.FeedbackTemplateId)
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
            .AddValidator(async ctx =>
            {
                // you can pass a non-you userid here, but if you do, you have to have Admin_View
                if (request.Request.UserId.IsNotEmpty() && request.Request.UserId != _actingUserService.Get().Id && !await _permissions.Can(PermissionKey.Admin_View))
                {
                    ctx.AddValidationException(new CantUpdateOtherUserFeedback(request.Request.UserId));
                }
            })
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

        // the user ID we're going to work on is the one in the request OR the logged in one if that's blank
        var updateForUserId = request.Request.UserId.IsEmpty() ? _actingUserService.Get().Id : request.Request.UserId;

        // we don't have them update by id since the user id + entity are a unique key
        // so load any previous submission to check for update
        var existingSubmission = await _feedbackService.ResolveExistingSubmission
        (
            updateForUserId,
            request.Request.AttachedEntity.EntityType,
            request.Request.AttachedEntity.Id,
            cancellationToken
        );

        // ultimately our retval
        var submissionModel = default(FeedbackSubmission);

        // if updating, update
        if (existingSubmission is not null)
        {
            submissionModel = await UpdateSubmission(existingSubmission, request.Request, cancellationToken);
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
                        UserId = updateForUserId,
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
                        UserId = updateForUserId,
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
            updateForUserId,
            request.Request.AttachedEntity.EntityType,
            request.Request.AttachedEntity.Id,
            cancellationToken
        );
    }

    private async Task<FeedbackSubmission> UpdateSubmission(FeedbackSubmissionView existingSubmission, UpsertFeedbackSubmissionRequest request, CancellationToken cancellationToken)
    {
        var submissionModel = default(FeedbackSubmission);

        // we also need the template so we can be sure to save an answer for every question, even if not supplied previously
        var template = await _store
            .WithNoTracking<FeedbackTemplate>()
            .Where(t => t.Id == request.FeedbackTemplateId)
            .SingleAsync(cancellationToken);

        await _store.DoTransaction(async dbContext =>
        {
            if (request.AttachedEntity.EntityType == FeedbackSubmissionAttachedEntityType.ChallengeSpec)
            {
                submissionModel = await _store
                    .WithTracking<FeedbackSubmissionChallengeSpec>()
                    .Where(s => s.Id == existingSubmission.Id)
                    .SingleAsync(cancellationToken);
            }
            else if (request.AttachedEntity.EntityType == FeedbackSubmissionAttachedEntityType.Game)
            {
                submissionModel = await _store
                    .WithTracking<FeedbackSubmissionGame>()
                    .Where(s => s.Id == existingSubmission.Id)
                    .SingleAsync(cancellationToken);
            }

            submissionModel.WhenEdited = _nowService.Get();

            foreach (var question in _feedbackService.BuildQuestionConfigFromTemplate(template).Questions)
            {
                var existingResponse = submissionModel.Responses.SingleOrDefault(r => r.Id == question.Id);
                if (existingResponse is not null)
                {
                    existingResponse.Answer = request.Responses.SingleOrDefault(r => r.Id == question.Id)?.Answer;
                    dbContext.Entry(existingResponse).Property(r => r.Answer).IsModified = true;
                }
                else
                {
                    submissionModel.Responses.Add(new QuestionSubmission
                    {
                        Id = question.Prompt,
                        Answer = request.Responses.SingleOrDefault(r => r.Id == question.Id)?.Answer,
                        Prompt = question.Prompt,
                        ShortName = question.ShortName,
                    });
                }
            }

            if (request.IsFinalized && submissionModel.WhenFinalized is null)
            {
                submissionModel.WhenFinalized = _nowService.Get();
            }

            await dbContext.SaveChangesAsync(cancellationToken);
        }, cancellationToken);

        return submissionModel;
    }
}
