using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Common.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Reports;

public record GetPlayersReportQuery(PlayersReportParameters Parameters, PagingArgs PagingArgs) : IRequest<ReportResults<PlayersReportStatSummary, PlayersReportRecord>>, IReportQuery;

internal class GetPlayersReportHandler : IRequestHandler<GetPlayersReportQuery, ReportResults<PlayersReportStatSummary, PlayersReportRecord>>
{
    private readonly INowService _nowService;
    private readonly IPagingService _pagingService;
    private readonly ReportsQueryValidator _queryValidator;
    private readonly IPlayersReportService _reportService;
    private readonly IReportsService _reportsService;

    public GetPlayersReportHandler
    (
        INowService nowService,
        IPagingService pagingService,
        ReportsQueryValidator queryValidator,
        IPlayersReportService reportService,
        IReportsService reportsService
    )
    {
        _nowService = nowService;
        _pagingService = pagingService;
        _queryValidator = queryValidator;
        _reportService = reportService;
        _reportsService = reportsService;
    }

    public async Task<ReportResults<PlayersReportStatSummary, PlayersReportRecord>> Handle(GetPlayersReportQuery request, CancellationToken cancellationToken)
    {
        // validate/authorize
        await _queryValidator.Validate(request, cancellationToken);

        var results = await _reportService
            .GetQuery(request.Parameters)
            .ToArrayAsync(cancellationToken);

        var statSummary = new PlayersReportStatSummary
        {
            UserCount = results.Length,
            UsersWithCompletedCompetitiveChallengeCount = results.Where(r => r.CompletedCompetitiveChallengesCount > 0).Count(),
            UsersWithCompletedPracticeChallengeCount = results.Where(r => r.CompletedPracticeChallengesCount > 0).Count(),
            UsersWithDeployedCompetitiveChallengeCount = results.Where(r => r.DeployedCompetitiveChallengesCount > 0).Count(),
            UsersWithDeployedPracticeChallengeCount = results.Where(r => r.DeployedPracticeChallengesCount > 0).Count()
        };
        var paged = _pagingService.Page(results, request.PagingArgs);


        return new ReportResults<PlayersReportStatSummary, PlayersReportRecord>
        {
            MetaData = new ReportMetaData
            {
                Key = ReportKey.Players,
                Title = "Players Report",
                Description = await _reportsService.GetDescription(ReportKey.Players),
                ParametersSummary = null,
                RunAt = _nowService.Get()
            },
            Paging = paged.Paging,
            Records = paged.Items,
            OverallStats = statSummary
        };
    }
}
