using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Gameboard.Api.Common;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Challenges;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Practice;

public record SearchPracticeChallengesQuery(SearchFilter Filter) : IRequest<SearchPracticeChallengesResult>;

internal class SearchPracticeChallengesHandler : IRequestHandler<SearchPracticeChallengesQuery, SearchPracticeChallengesResult>
{
    private readonly IChallengeDocsService _challengeDocsService;
    private readonly IMapper _mapper;
    private readonly IPagingService _pagingService;
    private readonly IStore _store;

    public SearchPracticeChallengesHandler
    (
        IChallengeDocsService challengeDocsService,
        IMapper mapper,
        IPagingService pagingService,
        IStore store
    )
    {
        _challengeDocsService = challengeDocsService;
        _mapper = mapper;
        _pagingService = pagingService;
        _store = store;
    }

    public async Task<SearchPracticeChallengesResult> Handle(SearchPracticeChallengesQuery request, CancellationToken cancellationToken)
    {
        var q = _store
            .List<Data.ChallengeSpec>()
            .Include(s => s.Game)
            .Where(s => s.Game.PlayerMode == PlayerMode.Practice);

        if (request.Filter.HasTerm)
        {
            var term = request.Filter.Term.ToLower();
            q = q.Where(s =>
                s.Id.Equals(term) ||
                s.Name.ToLower().Contains(term) ||
                s.Description.ToLower().Contains(term) ||
                s.Game.Name.ToLower().Contains(term) ||
                s.Text.ToLower().Contains(term)
            );
        }

        q = q.OrderBy(s => s.Name);
        var results = await _mapper.ProjectTo<ChallengeSpecSummary>(q).ToArrayAsync(cancellationToken);

        // fix up relative urls
        foreach (var result in results)
        {
            result.Text = _challengeDocsService.ReplaceRelativeUris(result.Text);
        }

        // resolve paging arguments
        var pageSize = request.Filter.Take > 0 ? request.Filter.Take : 100;
        var pageNumber = request.Filter.Skip / pageSize;

        var pagedResults = _pagingService.Page(results, new PagingArgs
        {
            PageNumber = pageNumber,
            PageSize = pageSize
        });

        return new SearchPracticeChallengesResult
        {
            Results = pagedResults
        };
    }
}
