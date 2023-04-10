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
    private readonly IGameService _gameService;
    private readonly IMediator _mediator;
    private readonly EntityExistsValidator<UpdatePlayerReadyStateCommand, Data.Player> _playerExists;
    private readonly PlayerService _playerService;
    private readonly IValidatorService<UpdatePlayerReadyStateCommand> _validatorService;

    public UpdatePlayerReadyStateCommandHandler(
        IGameService gameService,
        IMediator mediator,
        EntityExistsValidator<UpdatePlayerReadyStateCommand, Data.Player> playerExists,
        PlayerService playerService,
        IValidatorService<UpdatePlayerReadyStateCommand> validatorService)
    {
        _gameService = gameService;
        _mediator = mediator;
        _playerExists = playerExists;
        _playerService = playerService;
        _validatorService = validatorService;
    }

    public async Task Handle(UpdatePlayerReadyStateCommand request, CancellationToken cancellationToken)
    {
        // validate
        _validatorService.AddValidator(_playerExists.UseProperty(c => c.PlayerId));
        await _validatorService.Validate(request);

        // update the player's db flag
        await _playerService.UpdatePlayerReadyState(request.PlayerId, request.IsReady);

        // retrieve and tell the game that someone has readied/unreadied
        var player = await _playerService.Retrieve(request.PlayerId);
        await _gameService.HandleSyncStartStateChanged(player.GameId, request.Actor);
    }
}
