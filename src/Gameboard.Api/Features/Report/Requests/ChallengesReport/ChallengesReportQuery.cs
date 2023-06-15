using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Services;
using MediatR;

namespace Gameboard.Api.Features.Reports;

public record ChallengesReportQuery(GetChallengesReportQueryArgs Args) : IRequest<ReportResults<ChallengesReportRecord>>;

public class ChallengeReportQueryHandler : IRequestHandler<ChallengesReportQuery, ReportResults<ChallengesReportRecord>>
{
    private readonly INowService _now;
    private readonly IReportsService _reportsService;

    public ChallengeReportQueryHandler(INowService now, IReportsService reportsService)
    {
        _now = now;
        _reportsService = reportsService;
    }

    public async Task<ReportResults<ChallengesReportRecord>> Handle(ChallengesReportQuery request, CancellationToken cancellationToken)
    {
        var results = await _reportsService.GetChallengesReportRecords(request.Args);

        return new ReportResults<ChallengesReportRecord>
        {
            MetaData = new ReportMetaData
            {
                Key = ReportKey.ChallengesReport,
                Title = "Challenge Report",
                RunAt = _now.Get()
            },
            Records = results
        };
    }
}
