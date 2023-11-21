using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Data;
using Gameboard.Api.Structure.MediatR;
using Gameboard.Api.Structure.MediatR.Authorizers;
using Gameboard.Api.Structure.MediatR.Validators;
using Gameboard.Api.Validation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Games;

public record UpdatePlayerReadyStateCommand(string PlayerId, bool IsReady, User Actor) : IRequest;

internal class UpdatePlayerReadyStateCommandHandler : IRequestHandler<UpdatePlayerReadyStateCommand>
{
    private readonly UserRoleAuthorizer _authorizer;
    private readonly IMediator _mediator;
    private readonly EntityExistsValidator<UpdatePlayerReadyStateCommand, Data.Player> _playerExists;
    private readonly IStore _store;
    private readonly ISyncStartGameService _syncStartGameService;
    private readonly IValidatorService _validatorService;

    public UpdatePlayerReadyStateCommandHandler
    (
        UserRoleAuthorizer authorizer,
        IMediator mediator,
        EntityExistsValidator<UpdatePlayerReadyStateCommand, Data.Player> playerExists,
        IStore store,
        ISyncStartGameService syncStartGameService,
        IValidatorServiceFactory validatorServiceFactory)
    {
        _authorizer = authorizer;
        _mediator = mediator;
        _playerExists = playerExists;
        _store = store;
        _syncStartGameService = syncStartGameService;
        _validatorService = validatorServiceFactory.Get();
    }

    public async Task Handle(UpdatePlayerReadyStateCommand request, CancellationToken cancellationToken)
    {
        // validate
        // grab the player, we need it later anyway
        var player = await _store
            .WithNoTracking<Data.Player>()
            .SingleOrDefaultAsync(p => p.Id == request.PlayerId);

        _validatorService.AddValidator(ctx =>
        {
            if (player == null)
                ctx.AddValidationException(new ResourceNotFound<Data.Player>(request.PlayerId));
        });
        await _validatorService.Validate();

        // authorize
        _authorizer
            .AllowRoles(UserRole.Designer, UserRole.Tester, UserRole.Admin)
            .AllowUserId(player.UserId);

        // update the player's db flag
        var playerReadyState = await _syncStartGameService.UpdatePlayerReadyState(request.PlayerId, request.IsReady, cancellationToken);

        // notify listeners
        await _syncStartGameService.HandleSyncStartStateChanged(player.GameId, cancellationToken);
    }
}
