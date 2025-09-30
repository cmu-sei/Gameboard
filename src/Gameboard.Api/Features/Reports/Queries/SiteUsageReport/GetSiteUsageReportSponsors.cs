// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Reports;

public sealed record GetSiteUsageReportSponsorsQuery(SiteUsageReportParameters Parameters) : IReportQuery, IRequest<IEnumerable<SiteUsageReportSponsor>>;

internal class GetSiteUsageReportSponsorsHandler : IRequestHandler<GetSiteUsageReportSponsorsQuery, IEnumerable<SiteUsageReportSponsor>>
{
    private readonly ISiteUsageReportService _reportService;
    private readonly IStore _store;
    private readonly ReportsQueryValidator _validator;

    public GetSiteUsageReportSponsorsHandler
    (
        ISiteUsageReportService reportService,
        IStore store,
        ReportsQueryValidator validator
    )
    {
        _reportService = reportService;
        _store = store;
        _validator = validator;
    }

    public async Task<IEnumerable<SiteUsageReportSponsor>> Handle(GetSiteUsageReportSponsorsQuery request, CancellationToken cancellationToken)
    {
        await _validator.Validate(request, cancellationToken);

        // this is all hinky because of the whole teamid thing, but we need the teamids playing, and then their players/sponsors
        var teamIds = await _reportService
            .GetBaseQuery(request.Parameters)
            .Select(c => c.TeamId)
            .Distinct()
            .ToArrayAsync(cancellationToken);

        return await _store
            .WithNoTracking<Data.Player>()
                .Include(p => p.Sponsor)
                    .ThenInclude(s => s.ParentSponsor)
            .Where(p => teamIds.Contains(p.TeamId))
            .GroupBy(p => new
            {
                Id = p.SponsorId,
                p.Sponsor.Name,
                p.Sponsor.Logo,
                ParentName = p.Sponsor.ParentSponsor != null ? p.Sponsor.ParentSponsor.Name : null
            })
            .Select(gr => new SiteUsageReportSponsor
            {
                Id = gr.Key.Id,
                Name = gr.Key.Name,
                Logo = gr.Key.Logo,
                ParentName = gr.Key.ParentName,
                PlayerCount = gr.Select(p => p.UserId).Distinct().Count()
            })
            .OrderByDescending(s => s.PlayerCount)
            .ToArrayAsync(cancellationToken);
    }
}
