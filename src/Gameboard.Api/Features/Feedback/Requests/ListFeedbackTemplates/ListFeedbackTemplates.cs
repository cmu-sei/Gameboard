using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Data;
using Gameboard.Api.Structure.MediatR;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Feedback;

public record ListFeedbackTemplatesQuery() : IRequest<ListFeedbackTemplatesResponse>;

internal sealed class ListFeedbackTemplatesHandler(IStore store, IValidatorService validatorService) : IRequestHandler<ListFeedbackTemplatesQuery, ListFeedbackTemplatesResponse>
{
    private readonly IStore _store = store;
    private readonly IValidatorService _validatorService = validatorService;

    public async Task<ListFeedbackTemplatesResponse> Handle(ListFeedbackTemplatesQuery request, CancellationToken cancellationToken)
    {
        await _validatorService
            .Auth(c => c.RequirePermissions(Users.PermissionKey.Games_CreateEditDelete))
            .Validate(cancellationToken);

        var templates = await _store
            .WithNoTracking<Data.FeedbackTemplate>()
            .Select(t => new FeedbackTemplateView
            {
                Id = t.Id,
                Content = t.Content,
                CreatedBy = new SimpleEntity { Id = t.CreatedByUserId, Name = t.CreatedByUser.ApprovedName },
                HelpText = t.HelpText,
                Name = t.Name,
                ResponseCount = 0,
                UseForGameChallenges = t.UseAsFeedbackTemplateForGameChallenges
                    .Select(s => new SimpleEntity { Id = s.Id, Name = s.Name })
                    .ToArray(),
                UseForGames = t.UseAsFeedbackTemplateForGames
                    .Select(g => new SimpleEntity { Id = g.Id, Name = g.Name })
                    .ToArray()
            })
            .ToArrayAsync(cancellationToken);

        return new ListFeedbackTemplatesResponse { Templates = templates };
    }
}
