using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Features.Games.External;
using Gameboard.Api.Structure.MediatR;
using Gameboard.Api.Structure.MediatR.Validators;
using MediatR;

namespace Gameboard.Api.Features.Admin;

public record GetExternalGameAdminContextRequest(string GameId) : IRequest<ExternalGameState>;

internal class GetExternalGameAdminContextHandler(
    IExternalGameService externalGameService,
    GameWithModeExistsValidator<GetExternalGameAdminContextRequest> gameExists,
    IValidatorService<GetExternalGameAdminContextRequest> validator
    ) : IRequestHandler<GetExternalGameAdminContextRequest, ExternalGameState>
{
    private readonly IExternalGameService _externalGameService = externalGameService;
    private readonly GameWithModeExistsValidator<GetExternalGameAdminContextRequest> _gameExists = gameExists;
    private readonly IValidatorService<GetExternalGameAdminContextRequest> _validator = validator;

    public async Task<ExternalGameState> Handle(GetExternalGameAdminContextRequest request, CancellationToken cancellationToken)
    {
        // authorize/validate
        await _validator
            .Auth(config => config.Require(Users.PermissionKey.Games_CreateEditDelete))
            .AddValidator
            (
                _gameExists
                    .UseIdProperty(r => r.GameId)
                    .WithEngineMode(GameEngineMode.External)
            )
            .Validate(request, cancellationToken);

        // do the thing!
        return await _externalGameService.GetExternalGameState(request.GameId, cancellationToken);
    }
}
