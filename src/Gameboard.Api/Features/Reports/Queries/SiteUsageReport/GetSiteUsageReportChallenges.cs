using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ServiceStack;

namespace Gameboard.Api.Features.Reports;

public sealed record GetSiteUsageReportChallengesQuery(SiteUsageReportParameters ReportParameters, PagingArgs PagingArgs) : IReportQuery, IRequest<PagedEnumerable<SiteUsageReportChallengeSpec>>;

internal sealed class GetSiteUsageReportChallengesHandler : IRequestHandler<GetSiteUsageReportChallengesQuery, PagedEnumerable<SiteUsageReportChallengeSpec>>
{
    private readonly IPagingService _pagingService;
    private readonly ISiteUsageReportService _reportService;
    private readonly IStore _store;
    private readonly ReportsQueryValidator _validator;

    public GetSiteUsageReportChallengesHandler
    (
        IPagingService pagingService,
        ISiteUsageReportService reportService,
        IStore store,
        ReportsQueryValidator validator
    )
    {
        _pagingService = pagingService;
        _reportService = reportService;
        _store = store;
        _validator = validator;
    }

    public async Task<PagedEnumerable<SiteUsageReportChallengeSpec>> Handle(GetSiteUsageReportChallengesQuery request, CancellationToken cancellationToken)
    {
        await _validator.Validate(request, cancellationToken);

        // have to do dumb joiny stuff because challengespec ARGH #317
        var specChallenges = await _reportService
            .GetBaseQuery(request.ReportParameters)
            .GroupBy(c => c.SpecId)
            .ToDictionaryAsync(cs => cs.Key, cs => cs.Select(c => new
            {
                c.Id,
                IsComplete = c.Score >= c.Points,
                IsPartial = c.Score > 0 && c.Score < c.Points
            }).ToArray(), cancellationToken);

        var specs = await _store
            .WithNoTracking<Data.ChallengeSpec>()
            .Where(cs => specChallenges.Keys.Contains(cs.Id))
            .Include(cs => cs.Game)
            .GroupBy(cs => new { cs.Id, cs.Name })
            .Select(gr => new SiteUsageReportChallengeSpec
            {
                Id = gr.Key.Id,
                Name = gr.Key.Name,
                // have to do these after because lookups inline cause SQL evaluation nonsense
                DeployCount = 0,
                SolveCompleteCount = 0,
                SolvePartialCount = 0,
                UsedInGames = gr.Select(cs => new SimpleEntity
                {
                    Id = cs.GameId,
                    Name = cs.Game.Name
                })
                .OrderBy(g => g.Name)
                .ToArray()
            })
            .OrderBy(cs => cs.Name)
            .ToArrayAsync(cancellationToken);

        foreach (var spec in specs)
        {
            spec.DeployCount = specChallenges[spec.Id].Length;
            spec.SolveCompleteCount = specChallenges[spec.Id].Where(c => c.IsComplete).Count();
            spec.SolvePartialCount = specChallenges[spec.Id].Where(c => c.IsPartial).Count();
        }

        return _pagingService.Page(specs, request.PagingArgs);
    }
}
