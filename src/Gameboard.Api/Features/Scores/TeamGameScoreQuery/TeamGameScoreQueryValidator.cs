using System.Threading.Tasks;
using Gameboard.Api.Data.Abstractions;
using Gameboard.Api.Structure.MediatR;
using Gameboard.Api.Structure.MediatR.Validators;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Scores;

internal class TeamGameScoreQueryValidator : IGameboardRequestValidator<TeamGameScoreQuery>
{
    private readonly IPlayerStore _playerStore;
    private readonly IValidatorService<TeamGameScoreQuery> _validatorService;

    public TeamGameScoreQueryValidator(
        IPlayerStore playerStore,
        IValidatorService<TeamGameScoreQuery> validatorService)
    {
        _playerStore = playerStore;
        _validatorService = validatorService;
    }

    public async Task Validate(TeamGameScoreQuery request)
    {
        // _validatorService.AddValidator(_teamExists);
        _validatorService.AddValidator(new SimpleValidator<TeamGameScoreQuery, string>
        {
            ValidationProperty = r => r.teamId,
            IsValid = async teamId =>
            {
                if (string.IsNullOrEmpty(teamId))
                    return false;

                var count = await _playerStore.List().CountAsync(p => p.TeamId == teamId);
                if (count == 0)
                {
                    return false;
                }

                return true;
            },
            ValidationFailureMessage = $"Can't retrieve score for team \"{request.teamId}\" - the team doesn't exist."
        });

        await _validatorService.Validate(request);
    }
}
