using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Services;
using Gameboard.Api.Structure.MediatR;
using Gameboard.Api.Structure.MediatR.Validators;
using MediatR;

namespace Gameboard.Api.Features.Games;

public record UpdatePlayerReadyStateCommand(string PlayerId, bool IsReady, User Actor) : IRequest;

internal class UpdatePlayerReadyStateCommandHandler : IRequestHandler<UpdatePlayerReadyStateCommand>
{
    private readonly IGameStartService _gameStartService;
    private readonly IMediator _mediator;
    private readonly EntityExistsValidator<UpdatePlayerReadyStateCommand, Data.Player> _playerExists;
    private readonly PlayerService _playerService;
    private readonly ISyncStartGameService _syncStartGameService;
    private readonly IValidatorService<UpdatePlayerReadyStateCommand> _validatorService;

    public UpdatePlayerReadyStateCommandHandler
    (
        IGameStartService gameStartService,
        IMediator mediator,
        EntityExistsValidator<UpdatePlayerReadyStateCommand, Data.Player> playerExists,
        PlayerService playerService,
        ISyncStartGameService syncStartGameService,
        IValidatorService<UpdatePlayerReadyStateCommand> validatorService)
    {
        _gameStartService = gameStartService;
        _mediator = mediator;
        _playerExists = playerExists;
        _playerService = playerService;
        _syncStartGameService = syncStartGameService;
        _validatorService = validatorService;
    }

    public async Task Handle(UpdatePlayerReadyStateCommand request, CancellationToken cancellationToken)
    {
        // validate
        _validatorService.AddValidator(_playerExists.UseProperty(c => c.PlayerId));
        await _validatorService.Validate(request);

        // update the player's db flag
        var playerReadyState = await _syncStartGameService.UpdatePlayerReadyState(request.PlayerId, request.IsReady);

        // notify listeners
        await _gameStartService.HandleSyncStartStateChanged(new SyncGameStartRequest
        {
            ActingUser = request.Actor,
            GameId = playerReadyState.GameId
        });
    }
}
