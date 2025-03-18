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

internal sealed class GetSiteUsageReportPlayersHandler
(
    ChallengeService challengeService,
    ILogger<GetSiteUsageReportPlayersHandler> logger,
    IPagingService pagingService,
    ISiteUsageReportService reportService,
    IStore store,
    ReportsQueryValidator validator
) : IRequestHandler<GetSiteUsageReportPlayersQuery, PagedEnumerable<SiteUsageReportPlayer>>
{
    public async Task<PagedEnumerable<SiteUsageReportPlayer>> Handle(GetSiteUsageReportPlayersQuery request, CancellationToken cancellationToken)
    {
        await validator.Validate(request, cancellationToken);

        // default to 20 per page if unspecified
        var paging = request.PagingArgs ?? new PagingArgs { PageNumber = 0, PageSize = 20 };
        paging.PageNumber ??= 0;
        paging.PageSize ??= 20;

        var challengeIdUserIdMaps = await challengeService
            .GetChallengeUserMaps(reportService.GetBaseQuery(request.ReportParameters), cancellationToken);
        var challengeQuery = store
            .WithNoTracking<Data.Challenge>()
            .Where(c => challengeIdUserIdMaps.ChallengeIdUserIds.Keys.Contains(c.Id))
            .Select(c => new
            {
                c.Id,
                c.PlayerMode,
                c.StartTime
            })
            .Distinct();

        var challengeData = await challengeQuery
            .ToDictionaryAsync(i => i.Id, i => new { i.Id, i.PlayerMode, i.StartTime }, cancellationToken);

        var competitiveChallengeIds = challengeData.Where(kv => kv.Value.PlayerMode == PlayerMode.Competition).Select(kv => kv.Key).ToArray();
        var practiceChallengeIds = challengeData.Where(kv => kv.Value.PlayerMode == PlayerMode.Practice).Select(kv => kv.Key).ToArray();

        var userQuery = store
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

        var userInfo = await userQuery
            .ToDictionaryAsync(gr => gr.Id, gr => gr, cancellationToken);

        // manually manage looping here because linq can be slow, believe it or not
        var finalUsers = new List<SiteUsageReportPlayer>();

        foreach (var uId in userInfo.Keys)
        {
            var userChallengeIds = challengeIdUserIdMaps.UserIdChallengeIds[uId];
            var user = new SiteUsageReportPlayer
            {
                UserId = uId,
                Name = userInfo[uId].Name,
                // performance issujes are related to the computation of the challenge counts. hard to do better without proper FKs, need to think on it.
                // ChallengeCountCompetitive = 0,
                // ChallengeCountPractice = 0,
                ChallengeCountCompetitive = userChallengeIds.Where(cId => competitiveChallengeIds.Contains(cId)).Count(),
                ChallengeCountPractice = userChallengeIds.Where(cId => practiceChallengeIds.Contains(cId)).Count(),
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
            };

            var userMatchesExclusiveModeCriteria =
            (
                request.PlayersParameters.ExclusiveToMode == null ||
                request.PlayersParameters.ExclusiveToMode == PlayerMode.Competition && (user.ChallengeCountCompetitive == 0 || user.ChallengeCountPractice > 0) ||
                request.PlayersParameters.ExclusiveToMode == PlayerMode.Practice && (user.ChallengeCountCompetitive > 0 || user.ChallengeCountPractice == 0)
            );

            if (userMatchesExclusiveModeCriteria)
            {
                finalUsers.Add(user);
            }
        }

        var enumeratedFinalUsers = finalUsers.OrderBy(u => u.Name).ToArray();

        return pagingService.Page(enumeratedFinalUsers, paging);
    }
}
