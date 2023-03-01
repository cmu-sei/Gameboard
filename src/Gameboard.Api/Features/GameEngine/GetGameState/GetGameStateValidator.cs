using System.Threading.Tasks;
using Gameboard.Api.Features.Player;
using Gameboard.Api.Structure;

namespace Gameboard.Api.Features.GameEngine;

public class GetGameStateValidator : IGameboardValidator<GetGameStateRequest>
{
    private readonly ITeamService _teamService;

    public GetGameStateValidator(ITeamService teamService)
    {
        _teamService = teamService;
    }

    public async Task<GameboardValidationException> Validate(GetGameStateRequest model)
    {
        if (string.IsNullOrWhiteSpace(model.TeamId))
            return new MissingRequiredInput(nameof(model.TeamId), model.TeamId);

        if (!(await _teamService.GetExists(model.TeamId)))
            return new ResourceNotFound<Team>(model.TeamId);

        return null;
    }
}
