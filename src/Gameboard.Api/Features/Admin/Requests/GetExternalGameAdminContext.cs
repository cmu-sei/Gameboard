using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Features.Games.External;
using Gameboard.Api.Structure.MediatR;
using Gameboard.Api.Structure.MediatR.Authorizers;
using Gameboard.Api.Structure.MediatR.Validators;
using MediatR;

namespace Gameboard.Api.Features.Admin;

public record GetExternalGameAdminContextRequest(string GameId) : IRequest<ExternalGameState>;

internal class GetExternalGameAdminContextHandler : IRequestHandler<GetExternalGameAdminContextRequest, ExternalGameState>
{
    private readonly IExternalGameService _externalGameService;
    private readonly GameWithModeExistsValidator<GetExternalGameAdminContextRequest> _gameExists;
    private readonly UserRoleAuthorizer _userRoleAuthorizer;
    private readonly IValidatorService<GetExternalGameAdminContextRequest> _validator;

    public GetExternalGameAdminContextHandler
    (
        IExternalGameService externalGameService,
        GameWithModeExistsValidator<GetExternalGameAdminContextRequest> gameExists,
        UserRoleAuthorizer userRoleAuthorizer,
        IValidatorService<GetExternalGameAdminContextRequest> validator
    )
    {
        _externalGameService = externalGameService;
        _gameExists = gameExists;
        _userRoleAuthorizer = userRoleAuthorizer;
        _validator = validator;
    }

    public async Task<ExternalGameState> Handle(GetExternalGameAdminContextRequest request, CancellationToken cancellationToken)
    {
        // authorize/validate
        _userRoleAuthorizer.AllowRoles(UserRole.Admin).Authorize();

        _validator.AddValidator
        (
            _gameExists
                .UseIdProperty(r => r.GameId)
                .WithEngineMode(GameEngineMode.External)
                .WithSyncStartRequired(true)
        );

        await _validator.Validate(request, cancellationToken);

        // do the thing!
        return await _externalGameService.GetExternalGameState(request.GameId, cancellationToken);
    }
}
