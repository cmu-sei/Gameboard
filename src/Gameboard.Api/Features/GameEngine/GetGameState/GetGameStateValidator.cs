using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Gameboard.Api.Structure;
using Gameboard.Api.Structure.MediatR;
using Gameboard.Api.Structure.MediatR.Validators;

namespace Gameboard.Api.Features.GameEngine.Requests;

internal class GetGameStateValidator : IGameboardRequestValidator<GetGameStateQuery>
{
    private readonly RequiredStringValidator _teamIdRequired;
    private readonly TeamExistsValidator _teamExists;

    public GetGameStateValidator(RequiredStringValidator requiredTeamId, TeamExistsValidator teamExists)
    {
        _teamExists = teamExists;
        _teamIdRequired = requiredTeamId;
    }

    public async Task<GameboardAggregatedValidationExceptions> Validate(GetGameStateQuery request)
    {
        var exceptions = new List<GameboardValidationException>()
            .AddIfNotNull(await _teamExists.Validate(request.teamId))
            .AddIfNotNull(await _teamIdRequired.Validate(new RequiredStringContext
            {
                PropertyName = request.teamId,
                Value = request.teamId
            }));

        if (exceptions.Count() > 0)
            return GameboardAggregatedValidationExceptions.FromValidationExceptions(exceptions);

        return null;
    }
}
