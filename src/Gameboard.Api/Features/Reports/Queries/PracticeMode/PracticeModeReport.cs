using System;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Common;
using MediatR;

namespace Gameboard.Api.Features.Reports;

public record PracticeModeReportQuery(PracticeModeReportParameters Parameters, PagingArgs PagingArgs) : IRequest<ReportResults<IPracticeModeReportRecord>>;

internal class PracticeModeReportHandler : IRequestHandler<PracticeModeReportQuery, ReportResults<IPracticeModeReportRecord>>
{
    private readonly IPracticeModeReportService _practiceModeByUserReportService;
    private readonly IReportsService _reportsService;

    public PracticeModeReportHandler
    (
        IPracticeModeReportService practiceModeReportService,
        IReportsService reportsService
    )
     => (_practiceModeByUserReportService, _reportsService) = (practiceModeReportService, reportsService);

    public async Task<ReportResults<IPracticeModeReportRecord>> Handle(PracticeModeReportQuery request, CancellationToken cancellationToken)
    {
        if (request.Parameters.Grouping == PracticeModeReportGrouping.Challenge)
            return _reportsService.BuildResults(new ReportRawResults<IPracticeModeReportRecord>
            {
                PagingArgs = request.PagingArgs,
                ParameterSummary = null,
                Records = await _practiceModeByUserReportService.GetResultsByChallenge(request.Parameters, cancellationToken),
                ReportKey = ReportKey.PracticeMode,
                Title = "Practice Mode Report (Grouped By Challenge)"
            });
        else if (request.Parameters.Grouping == PracticeModeReportGrouping.Player)
            return _reportsService.BuildResults(new ReportRawResults<IPracticeModeReportRecord>
            {
                PagingArgs = request.PagingArgs,
                ParameterSummary = null,
                Records = await _practiceModeByUserReportService.GetResultsByUser(request.Parameters, cancellationToken),
                ReportKey = ReportKey.PracticeMode,
                Title = "Practice Mode Report (Grouped By Player)",
            });

        throw new ArgumentException(message: $"""Grouping value "{request.Parameters.Grouping}" is unsupported.""", nameof(request.Parameters.Grouping));
    }
}
