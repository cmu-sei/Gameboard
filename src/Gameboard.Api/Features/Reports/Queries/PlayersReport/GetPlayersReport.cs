using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Structure.MediatR;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Reports;

public record GetPlayersReportQuery(PlayersReportParameters Parameters, PagingArgs PagingArgs, User ActingUser) : IRequest<ReportResults<PlayersReportRecord>>, IReportQuery;

internal class GetPlayersReportHandler : IRequestHandler<GetPlayersReportQuery, ReportResults<PlayersReportRecord>>
{
    private readonly INowService _nowService;
    private readonly IPagingService _pagingService;
    private readonly ReportsQueryValidator _queryValidator;
    private readonly IPlayersReportService _reportService;
    private readonly IValidatorService<GetPlayersReportQuery> _validatorService;

    public GetPlayersReportHandler
    (
        INowService nowService,
        IPagingService pagingService,
        ReportsQueryValidator queryValidator,
        IPlayersReportService reportService,
        IValidatorService<GetPlayersReportQuery> validatorService
    )
    {
        _nowService = nowService;
        _pagingService = pagingService;
        _queryValidator = queryValidator;
        _reportService = reportService;
        _validatorService = validatorService;
    }

    public async Task<ReportResults<PlayersReportRecord>> Handle(GetPlayersReportQuery request, CancellationToken cancellationToken)
    {
        // validate/authorize
        await _queryValidator.Validate(request, cancellationToken);

        var results = await _reportService
            .GetQuery(request.Parameters)
            .ToArrayAsync(cancellationToken);

        var paged = _pagingService.Page(results, request.PagingArgs);

        return new ReportResults<PlayersReportRecord>
        {
            MetaData = new ReportMetaData
            {
                Key = ReportKey.Players,
                Title = "Players Report",
                ParametersSummary = null,
                RunAt = _nowService.Get()
            },
            Paging = paged.Paging,
            Records = paged.Items
        };
    }
}
