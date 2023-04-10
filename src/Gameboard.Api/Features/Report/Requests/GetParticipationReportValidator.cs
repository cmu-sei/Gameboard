using System;
using System.Threading.Tasks;
using Gameboard.Api.Structure;
using Gameboard.Api.Structure.MediatR;

namespace Gameboard.Api.Features.Reports;

internal class GetParticipationReportValidator : IGameboardRequestValidator<GetParticipationReportQuery>
{
    private readonly IValidatorService _validatorService;

    public GetParticipationReportValidator(IValidatorService validatorService)
    {
        _validatorService = validatorService;
    }

    public Task<GameboardAggregatedValidationExceptions> Validate(GetParticipationReportQuery input)
    {
        throw new NotImplementedException();
    }
}
