using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Gameboard.Api.Structure;
using Gameboard.Api.Structure.MediatR;
using Gameboard.Api.Structure.MediatR.Validators;

namespace Gameboard.Api.Features.GameEngine.Requests;

internal class GetGameStateValidator : IGameboardRequestValidator<GetGameStateQuery>
{
    private readonly RequiredStringValidator _requiredTeamId;
    private readonly TeamExistsValidator _teamExists;

    public GetGameStateValidator(RequiredStringValidator requiredTeamId, TeamExistsValidator teamExists)
    {
        _requiredTeamId = requiredTeamId;
        _teamExists = teamExists;

        _requiredTeamId.NameOfStringProperty = "teamId";
    }

    public async Task<GameboardAggregatedValidationExceptions> Validate(GetGameStateQuery request)
    {
        _requiredTeamId.NameOfStringProperty = nameof(request.teamId);
        var exceptions = new List<GameboardValidationException>();

        // TODO: why does this null ref?
        // var missingTeamId = await _requiredTeamId.Validate(request.teamId);
        // if (missingTeamId != null)
        //     exceptions.Add(missingTeamId);

        var teamDoesntExist = await _teamExists.Validate(request.teamId);
        if (teamDoesntExist != null)
            exceptions.Add(teamDoesntExist);

        if (exceptions.Count() > 0)
            return GameboardAggregatedValidationExceptions.FromValidationExceptions(exceptions);

        return null;
    }
}
