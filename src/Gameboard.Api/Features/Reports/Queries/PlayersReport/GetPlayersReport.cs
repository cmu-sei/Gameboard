using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Structure.MediatR;
using MediatR;

namespace Gameboard.Api.Features.Reports;

public record GetPlayersReportQuery(PlayersReportParameters Parameters, Data.User ActingUser) : IRequest<ReportResults<PlayersReportRecord>>;

internal class GetPlayersReportHandler : IRequestHandler<GetPlayersReportQuery, ReportResults<PlayersReportRecord>>
{
    private readonly IValidatorService<GetPlayersReportQuery> _validatorService;
    public Task<ReportResults<PlayersReportRecord>> Handle(GetPlayersReportQuery request, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }
}
