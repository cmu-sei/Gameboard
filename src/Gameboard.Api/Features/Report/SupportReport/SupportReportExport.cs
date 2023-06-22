using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;

namespace Gameboard.Api.Features.Reports;

public record SupportReportExportQuery(SupportReportParameters Parameters) : IRequest<IEnumerable<SupportReportExportRecord>>;

internal class SupportReportExportQueryHandler : IRequestHandler<SupportReportExportQuery, IEnumerable<SupportReportExportRecord>>
{
    private readonly IMapper _mapper;
    private readonly ISupportReportService _service;

    public SupportReportExportQueryHandler(IMapper mapper, ISupportReportService service)
    {
        _mapper = mapper;
        _service = service;
    }

    public async Task<IEnumerable<SupportReportExportRecord>> Handle(SupportReportExportQuery request, CancellationToken cancellationToken)
        => _mapper.Map<IEnumerable<SupportReportExportRecord>>(await _service.QueryRecords(request.Parameters));
}
