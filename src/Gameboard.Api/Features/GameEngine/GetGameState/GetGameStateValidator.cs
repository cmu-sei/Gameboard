using System.Threading.Tasks;
using Gameboard.Api.Data.Abstractions;
using Gameboard.Api.Structure;
using Gameboard.Api.Structure.MediatR;
using Gameboard.Api.Structure.MediatR.Validators;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.GameEngine.Requests;

internal class GetGameStateValidator : IGameboardRequestValidator<GetGameStateQuery>
{
    private readonly IPlayerStore _playerStore;
    private readonly RequiredStringValidator _teamIdRequired;
    private readonly IValidatorService<GetGameStateQuery> _validatorService;

    public GetGameStateValidator
    (
        RequiredStringValidator requiredTeamId,
        IPlayerStore playerStore,
        IValidatorService<GetGameStateQuery> validatorService
    )
    {
        _playerStore = playerStore;
        _teamIdRequired = requiredTeamId;
        _validatorService = validatorService;
    }

    public async Task Validate(GetGameStateQuery request)
    {
        _validatorService.AddValidator(new SimpleValidator<GetGameStateQuery, string>
        {
            ValidationProperty = r => r.TeamId,
            IsValid = async teamId => (await _playerStore
                .ListTeam(teamId)
                .CountAsync()) > 0,
            ValidationFailureMessage = $"Can't get game state for team \"{request.TeamId}\" - it doesn't exist."
        });

        await _validatorService.Validate(request);
    }
}
