using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Games.Start;
using Gameboard.Api.Structure.MediatR;
using Gameboard.Api.Structure.MediatR.Authorizers;
using Gameboard.Api.Structure.MediatR.Validators;
using Gameboard.Api.Validation;
using MediatR;

namespace Gameboard.Api.Features.Games;

public record UpdatePlayerReadyStateCommand(string PlayerId, bool IsReady, User Actor) : IRequest;

internal class UpdatePlayerReadyStateCommandHandler : IRequestHandler<UpdatePlayerReadyStateCommand>
{
    private readonly UserRoleAuthorizer _authorizer;
    private readonly IGameStartService _gameStartService;
    private readonly IMediator _mediator;
    private readonly EntityExistsValidator<UpdatePlayerReadyStateCommand, Data.Player> _playerExists;
    private readonly IStore _store;
    private readonly ISyncStartGameService _syncStartGameService;
    private readonly IValidatorService _validatorService;

    public UpdatePlayerReadyStateCommandHandler
    (
        UserRoleAuthorizer authorizer,
        IGameStartService gameStartService,
        IMediator mediator,
        EntityExistsValidator<UpdatePlayerReadyStateCommand, Data.Player> playerExists,
        IStore store,
        ISyncStartGameService syncStartGameService,
        IValidatorServiceFactory validatorServiceFactory)
    {
        _authorizer = authorizer;
        _gameStartService = gameStartService;
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
        var player = await _store.SingleOrDefaultAsync<Data.Player>(p => p.Id == request.PlayerId, cancellationToken);

        _validatorService.AddValidator(ctx =>
        {
            if (player == null)
                ctx.AddValidationException(new ResourceNotFound<Data.Player>(request.PlayerId));
        });
        await _validatorService.Validate();

        // authorize
        if (player.UserId != request.Actor.Id)
        {
            _authorizer
                .AllowRoles(UserRole.Designer, UserRole.Tester, UserRole.Admin)
                .Authorize();
        }

        // update the player's db flag
        var playerReadyState = await _syncStartGameService.UpdatePlayerReadyState(request.PlayerId, request.IsReady);

        // notify listeners
        await _gameStartService.HandleSyncStartStateChanged(player.GameId, cancellationToken);
    }
}
