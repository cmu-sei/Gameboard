using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Data;
using Gameboard.Api.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Gameboard.Api.Features.Reports;

public record GetSiteUsageReportPlayersQuery(SiteUsageReportParameters ReportParameters, SiteUsageReportPlayersParameters PlayersParameters, PagingArgs PagingArgs) : IRequest<PagedEnumerable<SiteUsageReportPlayer>>, IReportQuery;

internal sealed class GetSiteUsageReportPlayersHandler : IRequestHandler<GetSiteUsageReportPlayersQuery, PagedEnumerable<SiteUsageReportPlayer>>
{
    private readonly ChallengeService _challengeService;
    private readonly ILogger<GetSiteUsageReportPlayersHandler> _logger;
    private readonly IPagingService _pagingService;
    private readonly ISiteUsageReportService _reportService;
    private readonly IStore _store;
    private readonly ReportsQueryValidator _validator;

    public GetSiteUsageReportPlayersHandler
    (
        ChallengeService challengeService,
        ILogger<GetSiteUsageReportPlayersHandler> logger,
        IPagingService pagingService,
        ISiteUsageReportService reportService,
        IStore store,
        ReportsQueryValidator validator
    )
    {
        _challengeService = challengeService;
        _logger = logger;
        _pagingService = pagingService;
        _reportService = reportService;
        _store = store;
        _validator = validator;
    }

    public async Task<PagedEnumerable<SiteUsageReportPlayer>> Handle(GetSiteUsageReportPlayersQuery request, CancellationToken cancellationToken)
    {
        await _validator.Validate(request, cancellationToken);

        // default to 20 per page if unspecified
        var paging = request.PagingArgs ?? new PagingArgs { PageNumber = 0, PageSize = 3 };
        paging.PageNumber ??= 0;
        paging.PageSize ??= 20;

        var challengeIdUserIdMaps = await _challengeService.GetChallengeUserMaps(_reportService.GetBaseQuery(request.ReportParameters), cancellationToken);
        var challengeQuery = _store
            .WithNoTracking<Data.Challenge>()
            .Where(c => challengeIdUserIdMaps.ChallengeIdUserIds.Keys.Contains(c.Id))
            .Select(c => new
            {
                c.Id,
                c.PlayerMode,
                c.StartTime
            })
            .Distinct();

        // timer for performance tracking (see https://github.com/cmu-sei/Gameboard/issues/480)
        var challengeQueryStopwatch = new Stopwatch();
        challengeQueryStopwatch.Start();

        var challengeData = await challengeQuery
            .ToDictionaryAsync(i => i.Id, i => new { i.Id, i.PlayerMode, i.StartTime }, cancellationToken);

        challengeQueryStopwatch.Stop();
        if (challengeQueryStopwatch.Elapsed.Seconds >= 10)
        {
            _logger.LogWarning($"GetSiteUsageReportPlayers handler - challenge query - total runtime in MS: {challengeQueryStopwatch.ElapsedMilliseconds}");
            _logger.LogWarning($"Query: {challengeQuery.ToQueryString()}");
        }

        var competitiveChallengeIds = challengeData.Where(kv => kv.Value.PlayerMode == PlayerMode.Competition).Select(kv => kv.Key).ToArray();
        var practiceChallengeIds = challengeData.Where(kv => kv.Value.PlayerMode == PlayerMode.Practice).Select(kv => kv.Key).ToArray();

        var userQuery = _store
            .WithNoTracking<Data.User>()
            .Include(u => u.Sponsor)
            .Where(u => challengeIdUserIdMaps.UserIdChallengeIds.Keys.Contains(u.Id))
            .Select(u => new
            {
                u.Id,
                Name = u.ApprovedName,
                u.SponsorId,
                SponsorName = u.Sponsor.Name,
                SponsorLogo = u.Sponsor.Logo
            });

        var userQueryStopwatch = new Stopwatch();
        userQueryStopwatch.Start();

        var userInfo = await userQuery
            .ToDictionaryAsync(gr => gr.Id, gr => gr, cancellationToken);

        userQueryStopwatch.Stop();
        if (userQueryStopwatch.Elapsed.Seconds >= 10)
        {
            _logger.LogWarning($"GetSiteUsageReportPlayers handler - user query - total runtime in MS: {userQueryStopwatch.ElapsedMilliseconds}");
            _logger.LogWarning($"Query: {userQuery.ToQueryString()}");
        }

        var finalUsers = userInfo.Keys.Select(uId => new SiteUsageReportPlayer
        {
            UserId = uId,
            Name = userInfo[uId].Name,
            ChallengeCountCompetitive = challengeIdUserIdMaps.UserIdChallengeIds[uId].Where(cId => competitiveChallengeIds.Contains(cId)).Count(),
            ChallengeCountPractice = challengeIdUserIdMaps.UserIdChallengeIds[uId].Where(cId => practiceChallengeIds.Contains(cId)).Count(),
            LastActive = challengeIdUserIdMaps
                .UserIdChallengeIds[uId]
                .Select(cId => challengeData[cId])
                .OrderByDescending(cData => cData.StartTime)
                .Select(cData => cData.StartTime)
                .First(),
            Sponsor = new SimpleSponsor
            {
                Id = userInfo[uId].SponsorId,
                Name = userInfo[uId].SponsorName,
                Logo = userInfo[uId].SponsorLogo
            }
        });

        if (request.PlayersParameters.ExclusiveToMode == PlayerMode.Competition)
            finalUsers = finalUsers.Where(u => u.ChallengeCountCompetitive > 0 && u.ChallengeCountPractice == 0);

        if (request.PlayersParameters.ExclusiveToMode == PlayerMode.Practice)
            finalUsers = finalUsers.Where(u => u.ChallengeCountCompetitive == 0 && u.ChallengeCountPractice > 0);

        finalUsers = finalUsers.OrderBy(u => u.Name);
        return _pagingService.Page(finalUsers, paging);
    }
}
