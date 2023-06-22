using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;

namespace Gameboard.Api.Features.Reports;

public record ChallengesReportExportQuery(GetChallengesReportQueryArgs Parameters) : IRequest<IEnumerable<ChallengesReportCsvRecord>>;

public class ChallengesReportExportQueryHandler : IRequestHandler<ChallengesReportExportQuery, IEnumerable<ChallengesReportCsvRecord>>
{
    private readonly IMapper _mapper;
    private readonly IReportsService _reportsService;

    public ChallengesReportExportQueryHandler(IMapper mapper, IReportsService reportsService)
    {
        _mapper = mapper;
        _reportsService = reportsService;
    }

    public async Task<IEnumerable<ChallengesReportCsvRecord>> Handle(ChallengesReportExportQuery request, CancellationToken cancellationToken)
    {
        var results = await _reportsService.GetChallengesReportRecords(request.Parameters);
        return _mapper.Map<IEnumerable<ChallengesReportCsvRecord>>(results);
    }
}
