using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Users;
using Gameboard.Api.Structure.MediatR;
using Gameboard.Api.Structure.MediatR.Validators;

using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Games;

public record UpdatePlayerReadyStateCommand(string PlayerId, bool IsReady, User Actor) : IRequest;

internal class UpdatePlayerReadyStateCommandHandler(
    IMediator mediator,
    EntityExistsValidator<UpdatePlayerReadyStateCommand, Data.Player> playerExists,
    IStore store,
    ISyncStartGameService syncStartGameService,
    IValidatorService validatorService) : IRequestHandler<UpdatePlayerReadyStateCommand>
{
    private readonly IMediator _mediator = mediator;
    private readonly EntityExistsValidator<UpdatePlayerReadyStateCommand, Data.Player> _playerExists = playerExists;
    private readonly IStore _store = store;
    private readonly ISyncStartGameService _syncStartGameService = syncStartGameService;
    private readonly IValidatorService _validatorService = validatorService;

    public async Task Handle(UpdatePlayerReadyStateCommand request, CancellationToken cancellationToken)
    {
        // validate
        // grab the player, we need it later anyway
        var player = await _store
            .WithNoTracking<Data.Player>()
            .SingleOrDefaultAsync(p => p.Id == request.PlayerId);

        await _validatorService
            .ConfigureAuthorization
            (
                config => config
                    .RequirePermissions(UserRolePermissionKey.Games_AdminExternal)
                    .UnlessUserIdIn(player?.UserId)

            ).AddValidator(ctx =>
            {
                if (player == null)
                    ctx.AddValidationException(new ResourceNotFound<Data.Player>(request.PlayerId));
            })
            .Validate(cancellationToken);

        // update the player's db flag
        // (the service also ensures that sync-start game events get raised)
        var playerReadyState = await _syncStartGameService.UpdatePlayerReadyState(request.PlayerId, request.IsReady, cancellationToken);
    }
}
