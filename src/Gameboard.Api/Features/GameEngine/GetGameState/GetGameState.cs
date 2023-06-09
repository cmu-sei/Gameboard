using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Structure.MediatR.Authorizers;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace Gameboard.Api.Features.GameEngine;

public record GetGameStateQuery(string TeamId) : IRequest<IEnumerable<GameEngineGameState>>;

internal class GetGameStateHandler : IRequestHandler<GetGameStateQuery, IEnumerable<GameEngineGameState>>
{
    private readonly User _actor;
    private readonly IGameEngineStore _gameEngineStore;
    private readonly GetGameStateValidator _validator;
    private readonly UserRoleAuthorizer _roleAuthorizer;

    public GetGameStateHandler
    (
        IGameEngineStore gameEngineStore,
        UserRoleAuthorizer roleAuthorizer,
        GetGameStateValidator validator,
        IHttpContextAccessor httpContextAccessor
    )
    {
        _actor = httpContextAccessor.HttpContext.User.ToActor();
        _gameEngineStore = gameEngineStore;
        _roleAuthorizer = roleAuthorizer;
        _validator = validator;
    }

    public async Task<IEnumerable<GameEngineGameState>> Handle(GetGameStateQuery request, CancellationToken cancellationToken)
    {
        _roleAuthorizer
            .AllowRoles(UserRole.Admin, UserRole.Designer, UserRole.Designer)
            .Authorize();

        await _validator.Validate(request);
        return await _gameEngineStore.GetGameStatesByTeam(request.TeamId);
    }
}
