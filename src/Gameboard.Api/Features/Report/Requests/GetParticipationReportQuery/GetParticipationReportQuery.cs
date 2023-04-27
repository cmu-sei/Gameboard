using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Gameboard.Api.Data.Abstractions;
using Gameboard.Api.Services;
using Gameboard.Api.Structure;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Reports;

public record GetParticipationReportQuery(ParticipationReportArgs Args) : IRequest<ParticipationReport>;

public class GetParticipationReportQueryHandler : IRequestHandler<GetParticipationReportQuery, ParticipationReport>
{
    private readonly IMapper _mapper;
    private readonly INowService _now;
    private readonly IPlayerStore _playerStore;

    public GetParticipationReportQueryHandler(
        IMapper mapper,
        INowService now,
        IPlayerStore playerStore)
    {
        _mapper = mapper;
        _now = now;
        _playerStore = playerStore;
    }

    // fundamental unit - player challenge
    public async Task<ParticipationReport> Handle(GetParticipationReportQuery request, CancellationToken cancellationToken)
    {
        // need to double-check whether or not stuff like this forces client eval if you move it into the query
        var args = request.Args;
        var hasDateEnd = args.DateRange?.DateEnd != null;
        var hasDateStart = args.DateRange?.DateStart != null;

        var filters = new List<Func<Data.Player, bool>>();

        if (args.ChallengeId.NotEmpty())
            filters.Add(p => p.Challenges.Any(c => c.Id == args.ChallengeId));

        if (args.Competition.NotEmpty())
            filters.Add(p => p.Game.Competition == args.Competition);

        if (args.DateRange?.DateEnd != null)
            filters.Add(p => p.SessionBegin <= args.DateRange.DateEnd);

        if (args.DateRange?.DateStart != null)
            filters.Add(p => p.SessionBegin >= args.DateRange.DateStart);

        if (args.GameId.NotEmpty())
            filters.Add(p => p.GameId == args.GameId);

        if (args.SponsorId.NotEmpty())
            filters.Add(p => p.Sponsor == args.SponsorId);

        if (args.TeamId.NotEmpty())
            filters.Add(p => p.TeamId == args.TeamId);

        if (args.Track.NotEmpty())
            filters.Add(p => p.Game.Track == args.Track);

        var baseQuery = _playerStore
            .List()
            .AsNoTracking()
            .Include(p => p.Game)
            .Include(p => p.User)
            .Include(p => p.Challenges)
            .AsQueryable();

        foreach (var filter in filters)
            baseQuery = baseQuery.Where(filter).AsQueryable();

        var results = await baseQuery.ToArrayAsync();

        return new ParticipationReport
        {
            MetaData = new ReportMetaData
            {
                Id = "tbd",
                Title = "Participation Report",
                RunAt = _now.Get()
            },
            Records = results.Select(r => new ParticipationReportRecord
            {
                // Challenge = _mapper.Map<SimpleEntity[]>(r.Challenges)
                Game = _mapper.Map<SimpleEntity>(r.Game),
                Player = _mapper.Map<SimpleEntity>(r),
                User = _mapper.Map<SimpleEntity>(r.User),
            })
        };
    }
}

