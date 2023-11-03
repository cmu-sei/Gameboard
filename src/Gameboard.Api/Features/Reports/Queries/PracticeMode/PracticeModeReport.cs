using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace Gameboard.Api.Features.Reports;

public record PracticeModeReportQuery(PracticeModeReportParameters Parameters, User ActingUser, PagingArgs PagingArgs) : IRequest<ReportResults<PracticeModeReportOverallStats, IPracticeModeReportRecord>>, IReportQuery;

internal class PracticeModeReportHandler : IRequestHandler<PracticeModeReportQuery, ReportResults<PracticeModeReportOverallStats, IPracticeModeReportRecord>>
{
    private readonly IPracticeModeReportService _practiceModeReportService;
    private readonly ReportsQueryValidator _reportsQueryValidator;
    private readonly IReportsService _reportsService;

    public PracticeModeReportHandler
    (
        IPracticeModeReportService practiceModeReportService,
        ReportsQueryValidator reportsQueryValidator,
        IReportsService reportsService
    )
     => (_practiceModeReportService, _reportsQueryValidator, _reportsService) = (practiceModeReportService, reportsQueryValidator, reportsService);

    public async Task<ReportResults<PracticeModeReportOverallStats, IPracticeModeReportRecord>> Handle(PracticeModeReportQuery request, CancellationToken cancellationToken)
    {
        // validate access for all reports
        await _reportsQueryValidator.Validate(request, cancellationToken);

        if (request.Parameters.Grouping == PracticeModeReportGrouping.Challenge)
        {
            var results = await _practiceModeReportService.GetResultsByChallenge(request.Parameters, cancellationToken);
            return _reportsService.BuildResults(new ReportRawResults<PracticeModeReportOverallStats, IPracticeModeReportRecord>
            {
                OverallStats = results.OverallStats,
                PagingArgs = request.PagingArgs,
                ParameterSummary = null,
                Records = results.Records,
                ReportKey = ReportKey.PracticeArea,
                Title = "Practice Area Report (Grouped By Challenge)"
            });
        }
        else if (request.Parameters.Grouping == PracticeModeReportGrouping.Player)
        {
            var results = await _practiceModeReportService.GetResultsByUser(request.Parameters, cancellationToken);
            return _reportsService.BuildResults(new ReportRawResults<PracticeModeReportOverallStats, IPracticeModeReportRecord>
            {
                OverallStats = results.OverallStats,
                PagingArgs = request.PagingArgs,
                ParameterSummary = null,
                Records = results.Records,
                ReportKey = ReportKey.PracticeArea,
                Title = "Practice Area Report (Grouped By Player)",
            });
        }
        else if (request.Parameters.Grouping == PracticeModeReportGrouping.PlayerModePerformance)
        {
            var results = await _practiceModeReportService.GetResultsByPlayerModePerformance(request.Parameters, cancellationToken);
            return _reportsService.BuildResults(new ReportRawResults<PracticeModeReportOverallStats, IPracticeModeReportRecord>
            {
                OverallStats = results.OverallStats,
                PagingArgs = request.PagingArgs,
                ParameterSummary = null,
                Records = results.Records,
                ReportKey = ReportKey.PracticeArea,
                Title = "Practice Area Report (Grouped By Player Mode Performance)"
            });
        }

        throw new ArgumentException(message: $"""Grouping value "{request.Parameters.Grouping}" is unsupported.""", nameof(request.Parameters.Grouping));
    }
}
