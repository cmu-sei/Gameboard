using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Data.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Reports;

public record GetPlayerReportQuery(PlayerReportQueryParameters Parameters) : IRequest<PlayerReportQueryResults>;

internal class GetPlayerReportQueryHandler : IRequestHandler<GetPlayerReportQuery, PlayerReportQueryResults>, IReportRequestHandler<PlayerReportQueryParameters, PlayerReportRecord, PlayerReportQueryResults>
{
    private readonly IPlayerStore _playerStore;

    public GetPlayerReportQueryHandler(IPlayerStore playerStore)
    {
        _playerStore = playerStore;
    }

    public async Task<PlayerReportQueryResults> Handle(GetPlayerReportQuery request, CancellationToken cancellationToken)
    {
        var query = BuildQuery(request.Parameters);
        var records = await FetchRecords(query);
        return await BuildResults(records);
    }

    public IQueryable<PlayerReportRecord> BuildQuery(PlayerReportQueryParameters parameters)
    {
        var baseQuery = _playerStore.ListWithNoTracking().AsQueryable();

        if (parameters.SessionStartWindow?.DateStart != null)
        {
            baseQuery = baseQuery.Where(p => p.SessionBegin >= parameters.SessionStartWindow.DateStart);
        }

        if (parameters.SessionStartWindow?.DateEnd != null)
        {
            baseQuery = baseQuery.Where(p => p.SessionBegin >= parameters.SessionStartWindow.DateEnd);
        }

        if (parameters.ChallengeId.NotEmpty())
        {
            baseQuery = baseQuery
                .Include(p => p.Challenges)
                .Where(c => c.Id == parameters.ChallengeId);
        }
        else
        {
            baseQuery = baseQuery
                .Include(p => p.Challenges);
        }

        if (parameters.GameId.NotEmpty())
            baseQuery = baseQuery
                .Where(p => p.GameId == parameters.GameId);

        return baseQuery.Select(p => new PlayerReportRecord());
    }

    public async Task<IEnumerable<PlayerReportRecord>> FetchRecords(IQueryable<PlayerReportRecord> query)
    {
        return await query.ToArrayAsync();
    }

    public async Task<PlayerReportQueryResults> BuildResults(IEnumerable<PlayerReportRecord> records)
    {
        // return new PlayerReportQueryResults
        // {
        //     MetaData = new ReportMetaData
        //     {

        //     }
        // };
        return await Task.FromResult<PlayerReportQueryResults>(null);
    }
}
