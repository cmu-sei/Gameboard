using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;

namespace Gameboard.Api.Features.Reports;

public record ChallengesReportExportQuery(GetChallengesReportQueryArgs Parameters, User ActingUser) : IRequest<IEnumerable<ChallengesReportCsvRecord>>, IReportQuery;

internal class ChallengesReportExportQueryHandler : IRequestHandler<ChallengesReportExportQuery, IEnumerable<ChallengesReportCsvRecord>>
{
    private readonly IMapper _mapper;
    private readonly IReportsService _reportsService;
    private readonly ReportsQueryValidator _reportsQueryValidator;

    public ChallengesReportExportQueryHandler
    (
        IMapper mapper,
        IReportsService reportsService,
        ReportsQueryValidator reportsQueryValidator
    )
    {
        _mapper = mapper;
        _reportsService = reportsService;
        _reportsQueryValidator = reportsQueryValidator;
    }

    public async Task<IEnumerable<ChallengesReportCsvRecord>> Handle(ChallengesReportExportQuery request, CancellationToken cancellationToken)
    {
        await _reportsQueryValidator.Validate(request);
        var results = await _reportsService.GetChallengesReportRecords(request.Parameters);
        return _mapper.Map<IEnumerable<ChallengesReportCsvRecord>>(results);
    }
}
