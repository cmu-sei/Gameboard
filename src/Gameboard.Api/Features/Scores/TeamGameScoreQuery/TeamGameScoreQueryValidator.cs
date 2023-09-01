using System.Threading.Tasks;
using Gameboard.Api.Structure.MediatR;
using Gameboard.Api.Structure.MediatR.Validators;

namespace Gameboard.Api.Features.Scores;

internal class TeamGameScoreQueryValidator : IGameboardRequestValidator<TeamGameScoreQuery>
{
    private readonly TeamExistsValidator<TeamGameScoreQuery> _teamExists;
    private readonly IValidatorService<TeamGameScoreQuery> _validatorService;

    public TeamGameScoreQueryValidator(
        TeamExistsValidator<TeamGameScoreQuery> teamExists,
        IValidatorService<TeamGameScoreQuery> validatorService)
    {
        _teamExists = teamExists;
        _validatorService = validatorService;
    }

    public async Task Validate(TeamGameScoreQuery request)
    {
        _validatorService.AddValidator(_teamExists.UseProperty(r => r.TeamId));
        await _validatorService.Validate(request);
    }
}
