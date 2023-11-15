using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Games.External;
using Gameboard.Api.Structure.MediatR;
using Gameboard.Api.Structure.MediatR.Authorizers;
using MediatR;

namespace Gameboard.Api.Features.Admin;

public record GetExternalGameAdminContextRequest(string GameId) : IRequest<ExternalGameAdminContext>;

internal class GetExternalGameAdminContextHandler : IRequestHandler<GetExternalGameAdminContextRequest, ExternalGameAdminContext>
{
    private readonly GameWithModeExistsValidator<GetExternalGameAdminContextRequest> _gameExists;
    private readonly IStore _store;
    private readonly UserRoleAuthorizer _userRoleAuthorizer;
    private readonly IValidatorService<GetExternalGameAdminContextRequest> _validator;

    public GetExternalGameAdminContextHandler
    (
        GameWithModeExistsValidator<GetExternalGameAdminContextRequest> gameExists,
        IStore store,
        UserRoleAuthorizer userRoleAuthorizer,
        IValidatorService<GetExternalGameAdminContextRequest> validator
    )
    {
        _gameExists = gameExists;
        _store = store;
        _userRoleAuthorizer = userRoleAuthorizer;
        _validator = validator;
    }

    public Task<ExternalGameAdminContext> Handle(GetExternalGameAdminContextRequest request, CancellationToken cancellationToken)
    {
        // authorize/validate
        _userRoleAuthorizer.AllowRoles(UserRole.Admin).Authorize();

        // _validator.AddValidator
        // (
        //     _gameExists
        //         .UseIdProperty(r => r.GameId)
        //         .WithEngineMode(GameEngineMode.External, r => r.)
        // );
        throw new System.NotImplementedException();
    }
}
