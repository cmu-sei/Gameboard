using System;
using System.Threading.Tasks;
using Gameboard.Api.Structure;
using Gameboard.Api.Structure.MediatR;

namespace Gameboard.Api.Features.Reports;

internal class GetParticipationReportValidator : IGameboardRequestValidator<GetParticipationReportQuery>
{
    private readonly IValidatorService<GetParticipationReportQuery> _validatorService;

    public GetParticipationReportValidator(IValidatorService<GetParticipationReportQuery> validatorService)
    {
        _validatorService = validatorService;
    }

    public Task Validate(GetParticipationReportQuery request)
    {
        throw new NotImplementedException();
    }
}
