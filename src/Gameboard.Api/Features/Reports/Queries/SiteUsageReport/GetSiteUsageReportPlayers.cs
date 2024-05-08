using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Data;
using Gameboard.Api.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ServiceStack;

namespace Gameboard.Api.Features.Reports;

public record GetSiteUsageReportPlayersQuery(SiteUsageReportParameters ReportParameters, SiteUsageReportPlayersParameters PlayersParameters, PagingArgs PagingArgs) : IRequest<PagedEnumerable<SiteUsageReportPlayer>>, IReportQuery;

internal sealed class GetSiteUsageReportPlayersHandler : IRequestHandler<GetSiteUsageReportPlayersQuery, PagedEnumerable<SiteUsageReportPlayer>>
{
    private readonly ChallengeService _challengeService;
    private readonly IPagingService _pagingService;
    private readonly ISiteUsageReportService _reportService;
    private readonly IStore _store;
    private readonly ReportsQueryValidator _validator;

    public GetSiteUsageReportPlayersHandler
    (
        ChallengeService challengeService,
        IPagingService pagingService,
        ISiteUsageReportService reportService,
        IStore store,
        ReportsQueryValidator validator
    )
    {
        _challengeService = challengeService;
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

        // var challenges = await _reportService
        //     .GetBaseQuery(request.ReportParameters)
        //     .Select(c => new
        //     {
        //         c.Id,
        //         c.TeamId,
        //         c.PlayerMode
        //     })
        //     .ToArrayAsync(cancellationToken);

        var challengeIdUserIdMaps = await _challengeService.GetChallengeUserMaps(_reportService.GetBaseQuery(request.ReportParameters), cancellationToken);
        var challengeModes = await _store
            .WithNoTracking<Data.Challenge>()
            .Where(c => challengeIdUserIdMaps.ChallengeIdUserIds.Keys.Contains(c.Id))
            .Select(c => new
            {
                c.Id,
                c.PlayerMode,
                c.StartTime
            })
            .Distinct()
            .ToDictionaryAsync(i => i.Id, i => new { i.PlayerMode, i.StartTime }, cancellationToken);

        var competitiveChallengeIds = challengeModes.Where(kv => kv.Value.PlayerMode == PlayerMode.Competition).Select(kv => kv.Key).ToArray();
        var practiceChallengeIds = challengeModes.Where(kv => kv.Value.PlayerMode == PlayerMode.Practice).Select(kv => kv.Key).ToArray();

        var userInfo = await _store
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
            })
            .Distinct()
            .ToDictionaryAsync(gr => gr.Id, gr => gr, cancellationToken);

        var omg = userInfo.Keys.Select(uId => new SiteUsageReportPlayer
        {
            UserId = uId,
            Name = userInfo[uId].Name,
            ChallengeCountCompetitive = challengeIdUserIdMaps.UserIdChallengeIds[uId].Where(cId => competitiveChallengeIds.Contains(cId)).Count(),
            ChallengeCountPractice = challengeIdUserIdMaps.UserIdChallengeIds[uId].Where(cId => practiceChallengeIds.Contains(cId)).Count(),
            LastActive = challengeIdUserIdMaps.UserIdChallengeIds[uId].OrderBy(c => )
            Sponsor = new SimpleSponsor
            {
                Id = userInfo[uId].SponsorId,
                Name = userInfo[uId].SponsorName,
                Logo = userInfo[uId].SponsorLogo
            }
        });

        var query = _reportService
            .GetBaseQuery(request.ReportParameters)
            //     .Include(c => c.Player)
            //         .ThenInclude(p => p.User)
            //             .ThenInclude(u => u.Sponsor)
            // .GroupBy(c => new
            // {
            //     c.Player.UserId,
            //     Name = c.Player.User.ApprovedName,
            //     c.Player.User.SponsorId,
            //     SponsorName = c.Player.User.Sponsor.Name,
            //     SponsorLogo = c.Player.User.Sponsor.Logo
            // })
            .Select(c => new SiteUsageReportPlayer
            {
                Name = userInfo.n
                ChallengeCountCompetitive = gr.Count(c => c.PlayerMode == PlayerMode.Competition),
                ChallengeCountPractice = gr.Count(c => c.PlayerMode == PlayerMode.Practice),
                LastActive = gr.Max(gr => gr.StartTime),
                Sponsor = new SimpleSponsor
                {
                    Id = gr.Key.SponsorId,
                    Logo = gr.Key.SponsorLogo,
                    Name = gr.Key.SponsorName
                },
                UserId = gr.Key.UserId
            })
            .Distinct();

        if (request.PlayersParameters.ExclusiveToMode == PlayerMode.Competition)
            query = query.Where(p => p.ChallengeCountCompetitive > 0 && p.ChallengeCountPractice == 0);

        if (request.PlayersParameters.ExclusiveToMode == PlayerMode.Practice)
            query = query.Where(p => p.ChallengeCountCompetitive == 0 && p.ChallengeCountPractice > 0);

        query = query.OrderBy(p => p.Name);

        var results = await query.ToArrayAsync(cancellationToken);
        return _pagingService.Page(query, paging);
    }
}
