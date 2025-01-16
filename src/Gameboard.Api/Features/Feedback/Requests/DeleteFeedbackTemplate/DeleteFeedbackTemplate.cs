using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Data;
using Gameboard.Api.Structure.MediatR;
using Gameboard.Api.Structure.MediatR.Validators;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Feedback;

public record DeleteFeedbackTemplateCommand(string FeedbackTemplateId) : IRequest;

internal sealed class DeleteFeedbackTemplateHandler
(
    IStore store,
    EntityExistsValidator<FeedbackTemplate> templateExists,
    IValidatorService validator
) : IRequestHandler<DeleteFeedbackTemplateCommand>
{
    private readonly IStore _store = store;
    private readonly EntityExistsValidator<FeedbackTemplate> _templateExists = templateExists;
    private readonly IValidatorService _validator = validator;

    public async Task Handle(DeleteFeedbackTemplateCommand request, CancellationToken cancellationToken)
    {
        await _validator
            .Auth(c => c.Require(Users.PermissionKey.Games_CreateEditDelete))
            .AddValidator(_templateExists.UseValue(request.FeedbackTemplateId))
            .Validate(cancellationToken);

        // unset all games using this template
        await _store
            .WithNoTracking<Data.Game>()
            .Where(g => g.FeedbackTemplateId == request.FeedbackTemplateId)
            .ExecuteUpdateAsync(up => up.SetProperty(g => g.FeedbackTemplateId, default(string)), cancellationToken);

        await _store
            .WithNoTracking<Data.Game>()
            .Where(g => g.ChallengesFeedbackTemplateId == request.FeedbackTemplateId)
            .ExecuteUpdateAsync(up => up.SetProperty(g => g.ChallengesFeedbackTemplateId, default(string)), cancellationToken);

        // and then delete it
        await _store
            .WithNoTracking<FeedbackTemplate>()
            .Where(t => t.Id == request.FeedbackTemplateId)
            .ExecuteDeleteAsync(cancellationToken);
    }
}
