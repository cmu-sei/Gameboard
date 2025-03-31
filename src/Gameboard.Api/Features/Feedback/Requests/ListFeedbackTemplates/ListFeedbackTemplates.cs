using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Users;
using Gameboard.Api.Structure.MediatR;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Feedback;

public record ListFeedbackTemplatesQuery() : IRequest<ListFeedbackTemplatesResponse>;

internal sealed class ListFeedbackTemplatesHandler(IMapper mapper, IStore store, IValidatorService validatorService) : IRequestHandler<ListFeedbackTemplatesQuery, ListFeedbackTemplatesResponse>
{
    private readonly IMapper _mapper = mapper;
    private readonly IStore _store = store;
    private readonly IValidatorService _validatorService = validatorService;

    public async Task<ListFeedbackTemplatesResponse> Handle(ListFeedbackTemplatesQuery request, CancellationToken cancellationToken)
    {
        await _validatorService
            // reports users are allowed here because of the filters
            .Auth(c => c.RequireOneOf(PermissionKey.Games_CreateEditDelete, PermissionKey.Reports_View))
            .Validate(cancellationToken);

        var templates = await _mapper
            .ProjectTo<FeedbackTemplateView>
            (
                _store
                    .WithNoTracking<FeedbackTemplate>()
                    .OrderBy(t => t.Name)
            )
            .ToArrayAsync(cancellationToken);

        return new ListFeedbackTemplatesResponse { Templates = templates };
    }
}
