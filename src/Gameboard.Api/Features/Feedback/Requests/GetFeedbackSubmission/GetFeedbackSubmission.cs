using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Data;
using Gameboard.Api.Services;
using Gameboard.Api.Structure.MediatR;
using MediatR;

namespace Gameboard.Api.Features.Feedback;

public record GetFeedbackSubmissionQuery(GetFeedbackSubmissionRequest Request) : IRequest<FeedbackSubmissionView>;

internal sealed class GetFeedbackSubmissionHandler
(
    IActingUserService actingUser,
    FeedbackService feedbackService,
    IStore store,
    IValidatorService validator
) : IRequestHandler<GetFeedbackSubmissionQuery, FeedbackSubmissionView>
{
    private readonly IActingUserService _actingUser = actingUser;
    private readonly FeedbackService _feedbackService = feedbackService;
    private readonly IStore _store = store;
    private readonly IValidatorService _validator = validator;

    public async Task<FeedbackSubmissionView> Handle(GetFeedbackSubmissionQuery request, CancellationToken cancellationToken)
    {
        await _validator
            .Auth
            (
                c => c
                    .RequireAuthentication()
                    .RequirePermissions(Users.PermissionKey.Admin_View)
                    .UnlessUserIdIn(request.Request.UserId)
            )
            .AddValidator(ctx =>
            {
                if (request.Request.EntityId.IsEmpty())
                {
                    ctx.AddValidationException(new MissingRequiredInput<string>(nameof(request.Request.EntityId)));
                }

                if (request.Request.UserId.IsEmpty())
                {
                    ctx.AddValidationException(new MissingRequiredInput<string>(nameof(request.Request.UserId)));
                }
            })
            .Validate(cancellationToken);

        return await _feedbackService.ResolveExistingSubmission(request.Request.UserId, request.Request.EntityType, request.Request.EntityId, cancellationToken);
    }
}
