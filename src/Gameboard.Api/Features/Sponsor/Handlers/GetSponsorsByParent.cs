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

public class GetSponsorsByParentResponse
{
    public required Sponsor DefaultSponsor { get; set; }
    public required IEnumerable<SponsorWithChildSponsors> ParentSponsors { get; set; }
    public required IEnumerable<Sponsor> NonParentSponsors { get; set; }
}

public record GetSponsorsByParentQuery() : IRequest<GetSponsorsByParentResponse>;

internal class GetSponsorsByParentHandler : IRequestHandler<GetSponsorsByParentQuery, GetSponsorsByParentResponse>
{
    private readonly IMapper _mapper;
    private readonly SponsorService _sponsorService;
    private readonly IStore _store;

    public GetSponsorsByParentHandler(IMapper mapper, SponsorService sponsorService, IStore store)
    {
        _mapper = mapper;
        _sponsorService = sponsorService;
        _store = store;
    }

    public async Task<GetSponsorsByParentResponse> Handle(GetSponsorsByParentQuery request, CancellationToken cancellationToken)
    {
        var allSponsors = await _store
            .WithNoTracking<Data.Sponsor>()
            .Include(s => s.ParentSponsor)
            .Include(s => s.ChildSponsors)
            .OrderBy(s => s.Name)
            .ToArrayAsync(cancellationToken);

        return new GetSponsorsByParentResponse
        {
            DefaultSponsor = _mapper.Map<Sponsor>(await _sponsorService.GetDefaultSponsor()),
            ParentSponsors = _mapper.Map<IEnumerable<SponsorWithChildSponsors>>(allSponsors.Where(s => s.ChildSponsors is not null && s.ChildSponsors.Count > 0)),
            NonParentSponsors = _mapper.Map<IEnumerable<Sponsor>>(allSponsors.Where(s => s.ParentSponsor is null && (s.ChildSponsors is null || !s.ChildSponsors.Any())))
        };
    }
}
