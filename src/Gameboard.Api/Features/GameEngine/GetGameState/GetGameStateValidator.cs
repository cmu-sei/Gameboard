using System.Linq;
using System.Threading.Tasks;
using Gameboard.Api.Data.Abstractions;
using Gameboard.Api.Features.Teams;
using Gameboard.Api.Structure.MediatR;
using Gameboard.Api.Structure.MediatR.Authorizers;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.GameEngine;

internal class GetGameStateValidator : IGameboardRequestValidator<GetGameStateQuery>
{
    private readonly User _actingUser;
    private readonly IPlayerStore _playerStore;
    private readonly UserRoleAuthorizer _roleAuthorizer;
    private readonly IValidatorService<GetGameStateQuery> _validatorService;

    public GetGameStateValidator
    (
        IHttpContextAccessor httpContextAccessor,
        IPlayerStore playerStore,
        UserRoleAuthorizer roleAuthorizer,
        IValidatorService<GetGameStateQuery> validatorService
    )
    {
        _actingUser = httpContextAccessor.HttpContext.User.ToActor();
        _playerStore = playerStore;
        _roleAuthorizer = roleAuthorizer;
        _validatorService = validatorService;
    }

    public async Task Validate(GetGameStateQuery request)
    {
        var players = await _playerStore
            .ListTeam(request.TeamId)
            .AsNoTracking()
            .ToArrayAsync();

        _validatorService.AddValidator((request, context) =>
        {
            if (!players.Any(p => p.UserId == _actingUser.Id))
                context.AddValidationException(new PlayerIsntOnTeam(_actingUser.Id, request.TeamId, "[unknown]"));
            else
                _roleAuthorizer
                    .AllowRoles(UserRole.Admin, UserRole.Designer, UserRole.Tester)
                    .Authorize();
        });

        _validatorService.AddValidator((request, context) =>
        {
            if (!players.Any())
                context.AddValidationException(new ResourceNotFound<Team>(request.TeamId));
        });

        await _validatorService.Validate(request);
    }
}
