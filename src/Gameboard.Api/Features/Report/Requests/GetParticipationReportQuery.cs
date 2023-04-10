using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace Gameboard.Api.Features.Reports;

public record GetParticipationReportQuery(ParticipationReportArgs Args) : IRequest<ParticipationReport>;

public class GetParticipationReportQueryHandler : IRequestHandler<GetParticipationReportQuery, ParticipationReport>
{
    public Task<ParticipationReport> Handle(GetParticipationReportQuery request, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }
}
