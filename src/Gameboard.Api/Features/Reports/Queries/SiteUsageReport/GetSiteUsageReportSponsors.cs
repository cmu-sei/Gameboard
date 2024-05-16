using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Reports;

public sealed record GetSiteUsageReportSponsorsQuery(SiteUsageReportParameters Parameters) : IReportQuery, IRequest<IEnumerable<SiteUsageReportSponsor>>;

internal class GetSiteUsageReportSponsorsHandler : IRequestHandler<GetSiteUsageReportSponsorsQuery, IEnumerable<SiteUsageReportSponsor>>
{
    private readonly ISiteUsageReportService _reportService;
    private readonly ReportsQueryValidator _validator;

    public GetSiteUsageReportSponsorsHandler
    (
        ISiteUsageReportService reportService,
        ReportsQueryValidator validator
    )
    {
        _reportService = reportService;
        _validator = validator;
    }

    public async Task<IEnumerable<SiteUsageReportSponsor>> Handle(GetSiteUsageReportSponsorsQuery request, CancellationToken cancellationToken)
    {
        await _validator.Validate(request, cancellationToken);

        return await _reportService
            .GetBaseQuery(request.Parameters)
            .Include(c => c.Player)
                .ThenInclude(p => p.Sponsor)
                    .ThenInclude(s => s.ParentSponsor)
            .GroupBy(c => new
            {
                Id = c.Player.SponsorId,
                c.Player.Sponsor.Name,
                c.Player.Sponsor.Logo,
                ParentName = c.Player.Sponsor.ParentSponsor != null ? c.Player.Sponsor.ParentSponsor.Name : null
            })
            .Select(gr => new SiteUsageReportSponsor
            {
                Id = gr.Key.Id,
                Name = gr.Key.Name,
                Logo = gr.Key.Logo,
                ParentName = gr.Key.ParentName,
                PlayerCount = gr.Select(thing => thing.Player.UserId).Distinct().Count()
            })
            .ToArrayAsync(cancellationToken);
    }
}
