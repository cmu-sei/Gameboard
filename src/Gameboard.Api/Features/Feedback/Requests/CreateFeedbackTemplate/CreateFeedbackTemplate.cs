using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Data;
using Gameboard.Api.Structure.MediatR;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Feedback;

public record CreateFeedbackTemplateCommand(CreateFeedbackTemplateRequest Template) : IRequest<FeedbackTemplateView>;

internal sealed class CreateFeedbackTemplateHandler
(
    IActingUserService actingUserService,
    IStore store,
    IValidatorService validatorService
) : IRequestHandler<CreateFeedbackTemplateCommand, FeedbackTemplateView>
{
    private readonly IActingUserService _actingUserService = actingUserService;
    private readonly IStore _store = store;
    private readonly IValidatorService _validatorService = validatorService;

    public async Task<FeedbackTemplateView> Handle(CreateFeedbackTemplateCommand request, CancellationToken cancellationToken)
    {
        await _validatorService
            .Auth(c => c.RequirePermissions(Users.PermissionKey.Games_CreateEditDelete))
            .AddValidator(request.Template.Name.IsEmpty(), new MissingRequiredInput<string>(nameof(request.Template.Name)))
            .AddValidator(request.Template.Content.IsEmpty(), new MissingRequiredInput<string>(nameof(request.Template.Content)))
            .AddValidator(async ctx =>
            {
                if (await _store.WithNoTracking<Data.FeedbackTemplate>().AnyAsync(t => t.Name == request.Template.Name))
                {
                    ctx.AddValidationException(new DuplicateFeedbackTemplateNameException(request.Template.Name));
                }
            })
            .Validate(cancellationToken);

        var template = await _store
            .Create(new FeedbackTemplate
            {
                Content = request.Template.Content.Trim(),
                CreatedByUserId = _actingUserService.Get().Id,
                Name = request.Template.Name.Trim(),
                UseAsFeedbackTemplateForGameChallenges = [],
                UseAsFeedbackTemplateForGames = []
            });

        return new FeedbackTemplateView
        {
            Id = template.Id,
            Content = template.Content,
            CreatedBy = new SimpleEntity { Id = template.CreatedByUserId, Name = _actingUserService.Get().ApprovedName },
            HelpText = template.HelpText,
            Name = template.Name,
            ResponseCount = 0,
            UseForGameChallenges = [],
            UseForGames = []
        };
    }
}
