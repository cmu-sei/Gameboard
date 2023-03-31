using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Structure.MediatR.Authorizers;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace Gameboard.Api.Features.GameEngine.Requests;

public record GetGameStateQuery(string TeamId) : IRequest<GameEngineGameState>;

internal class GetGameStateHandler : IRequestHandler<GetGameStateQuery, GameEngineGameState>
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

        _roleAuthorizer.AllowedRoles = new UserRole[] { UserRole.Admin, UserRole.Designer, UserRole.Designer };
    }

    public async Task<GameEngineGameState> Handle(GetGameStateQuery request, CancellationToken cancellationToken)
    {
        _roleAuthorizer.Authorize();
        await _validator.Validate(request);

        return await _gameEngineStore.GetGameStateByTeam(request.TeamId);
    }
}
