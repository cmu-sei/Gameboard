using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Teams;
using Gameboard.Api.Structure.MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.GameEngine;

internal class GetGameStateValidator(
    IActingUserService actingUserService,
    IPlayerStore playerStore,
    IValidatorService<GetGameStateQuery> validatorService
    ) : IGameboardRequestValidator<GetGameStateQuery>
{
    private readonly User _actingUser = actingUserService.Get();
    // TODO: replace playerstore with ITeamService
    private readonly IPlayerStore _playerStore = playerStore;
    private readonly IValidatorService<GetGameStateQuery> _validatorService = validatorService;

    public async Task Validate(GetGameStateQuery request, CancellationToken cancellationToken)
    {
        var players = await _playerStore
            .ListTeam(request.TeamId)
            .AsNoTracking()
            .ToArrayAsync(cancellationToken);

        await _validatorService
            .Auth
            (
                a => a
                    .RequirePermissions(Users.PermissionKey.Admin_View)
                    .Unless
                    (
                        () => _playerStore
                            .ListTeam(request.TeamId)
                            .AsNoTracking()
                            .AnyAsync(p => p.UserId == _actingUser.Id, cancellationToken),
                        new PlayerIsntOnTeam(_actingUser.Id, request.TeamId, "[unknown]")
                    )
            )
            .AddValidator((request, context) =>
            {
                if (players.Length == 0)
                    context.AddValidationException(new ResourceNotFound<Team>(request.TeamId));
            })
            .Validate(request, cancellationToken);
    }
}
