using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Gameboard.Api.Common.Services;
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
    private readonly IPracticeService _practiceService;
    private readonly ISlugService _slugger;
    private readonly IStore _store;

    public SearchPracticeChallengesHandler
    (
        IChallengeDocsService challengeDocsService,
        IMapper mapper,
        IPagingService pagingService,
        IPracticeService practiceService,
        ISlugService slugger,
        IStore store
    )
    {
        _challengeDocsService = challengeDocsService;
        _mapper = mapper;
        _pagingService = pagingService;
        _practiceService = practiceService;
        _slugger = slugger;
        _store = store;
    }

    public async Task<SearchPracticeChallengesResult> Handle(SearchPracticeChallengesQuery request, CancellationToken cancellationToken)
    {
        // load settings - we need these to make decisions about tag-based matches
        var settings = await _practiceService.GetSettings(cancellationToken);
        var sluggedSuggestedSearches = settings.SuggestedSearches.Select(search => _slugger.Get(search));

        var query = BuildQuery(request.Filter.Term, sluggedSuggestedSearches);
        var results = await _mapper.ProjectTo<ChallengeSpecSummary>(query).ToArrayAsync(cancellationToken);

        foreach (var result in results)
        {
            // hide tags which aren't in the "suggested searches" configured in the practice area
            // (this is because topo has lots of tags that aren't useful to players, so we only)
            // want to show them values in the suggested search
            result.Tags = result.Tags.Where(t => sluggedSuggestedSearches.Contains(_slugger.Get(t)));

            // fix up relative urls
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

    /// <summary>
    /// Load the transformed query results from the database. (Broken out into its own function for unit testing.)
    /// </summary>
    /// <param name="filterTerm"></param>
    /// <param name="sluggedSuggestedSearches"></param>
    /// <returns></returns>
    internal IQueryable<Data.ChallengeSpec> BuildQuery(string filterTerm, IEnumerable<string> sluggedSuggestedSearches)
    {
        var q = _store
            .WithNoTracking<Data.ChallengeSpec>()
            .Include(s => s.Game)
            .Where(s => s.Game.PlayerMode == PlayerMode.Practice)
            .Where(s => !s.Disabled);

        if (filterTerm.IsNotEmpty())
        {
            var term = filterTerm.ToLower();
            var sluggedTerm = _slugger.Get(term);

            q = q.Where
            (
                s =>
                    s.Id.Equals(term) ||
                    s.Name.ToLower().Contains(term) ||
                    s.Description.ToLower().Contains(term) ||
                    s.Game.Name.ToLower().Contains(term) ||
                    s.Text.ToLower().Contains(term) ||
                    (s.Tags.Contains(sluggedTerm) && sluggedSuggestedSearches.Contains(sluggedTerm))
            );
        }

        q = q.OrderBy(s => s.Name);
        return q;
    }
}
