using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;

namespace Gameboard.Api.Features.Reports;

public record GetChallengesReportExportQuery(GetChallengesReportQueryArgs Parameters) : IRequest<IEnumerable<ChallengesReportCsvRecord>>;

public class GetChallengesReportExportQueryHandler : IRequestHandler<GetChallengesReportExportQuery, IEnumerable<ChallengesReportCsvRecord>>
{
    private readonly IMapper _mapper;
    private readonly IReportsService _reportsService;

    public GetChallengesReportExportQueryHandler(IMapper mapper, IReportsService reportsService)
    {
        _mapper = mapper;
        _reportsService = reportsService;
    }

    public async Task<IEnumerable<ChallengesReportCsvRecord>> Handle(GetChallengesReportExportQuery request, CancellationToken cancellationToken)
    {
        var results = await _reportsService.GetChallengesReportRecords(request.Parameters);
        return _mapper.Map<IEnumerable<ChallengesReportCsvRecord>>(results);
    }
}
