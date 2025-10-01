// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Gameboard.Api.Data;
using Gameboard.Api.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Sponsors;

public record GetSponsorsWithChildrenQuery() : IRequest<IEnumerable<SponsorWithChildSponsors>>;

internal class GetSponsorsWithChildrenHander : IRequestHandler<GetSponsorsWithChildrenQuery, IEnumerable<SponsorWithChildSponsors>>
{
    private readonly IMapper _mapper;
    private readonly SponsorService _sponsorService;
    private readonly IStore _store;

    public GetSponsorsWithChildrenHander(IMapper mapper, SponsorService sponsorService, IStore store)
    {
        _mapper = mapper;
        _sponsorService = sponsorService;
        _store = store;
    }

    public async Task<IEnumerable<SponsorWithChildSponsors>> Handle(GetSponsorsWithChildrenQuery request, CancellationToken cancellationToken)
    {
        var allSponsors = await _store
            .WithNoTracking<Data.Sponsor>()
            .Include(s => s.ParentSponsor)
            .Include(s => s.ChildSponsors)
            .Where(s => s.ParentSponsorId == null)
            .OrderBy(s => s.Name)
            .ToArrayAsync(cancellationToken);

        return _mapper.Map<IEnumerable<SponsorWithChildSponsors>>(allSponsors).Select(s =>
        {
            s.ChildSponsors = s.ChildSponsors.OrderBy(s => s.Name).AsEnumerable();
            return s;
        });

    }
}
