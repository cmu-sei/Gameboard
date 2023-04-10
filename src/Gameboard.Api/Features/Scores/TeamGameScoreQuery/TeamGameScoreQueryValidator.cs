using System.Threading.Tasks;
using Gameboard.Api.Data.Abstractions;
using Gameboard.Api.Structure.MediatR;
using Gameboard.Api.Structure.MediatR.Validators;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Scores;

internal class TeamGameScoreQueryValidator : IGameboardRequestValidator<TeamGameScoreQuery>
{
    private readonly IPlayerStore _playerStore;
    private readonly TeamExistsValidator<TeamGameScoreQuery> _teamExists;
    private readonly IValidatorService<TeamGameScoreQuery> _validatorService;

    public TeamGameScoreQueryValidator(
        IPlayerStore playerStore,
        TeamExistsValidator<TeamGameScoreQuery> teamExists,
        IValidatorService<TeamGameScoreQuery> validatorService)
    {
        _playerStore = playerStore;
        _teamExists = teamExists;
        _validatorService = validatorService;
    }

    public async Task Validate(TeamGameScoreQuery request)
    {
        _teamExists.TeamIdProperty = r => r.teamId;
        _validatorService.AddValidator(_teamExists);


        await _validatorService.Validate(request);
    }
}
