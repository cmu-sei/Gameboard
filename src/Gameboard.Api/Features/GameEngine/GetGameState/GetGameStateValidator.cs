using System.Threading.Tasks;
using Gameboard.Api.Data.Abstractions;
using Gameboard.Api.Structure.MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.GameEngine;

internal class GetGameStateValidator : IGameboardRequestValidator<GetGameStateQuery>
{
    private readonly IPlayerStore _playerStore;
    private readonly IValidatorService<GetGameStateQuery> _validatorService;

    public GetGameStateValidator
    (
        IPlayerStore playerStore,
        IValidatorService<GetGameStateQuery> validatorService
    )
    {
        _playerStore = playerStore;
        _validatorService = validatorService;
    }

    public async Task Validate(GetGameStateQuery request)
    {
        _validatorService.AddValidator(async (request, context) =>
        {
            var count = await _playerStore
                .ListTeam(request.TeamId)
                .CountAsync();

            if (count == 0)
                context.AddValidationException(new ResourceNotFound<Team>(request.TeamId));
        });

        await _validatorService.Validate(request);
    }
}
