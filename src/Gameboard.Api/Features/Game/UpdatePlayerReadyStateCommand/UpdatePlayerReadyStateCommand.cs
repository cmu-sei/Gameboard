using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Data.Abstractions;
using Gameboard.Api.Structure.MediatR;
using Gameboard.Api.Structure.MediatR.Validators;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Games;

public record UpdatePlayerReadyStateCommand(string PlayerId, bool IsReady, User Actor) : IRequest;

internal class UpdatePlayerReadyStateCommandHandler : IRequestHandler<UpdatePlayerReadyStateCommand>
{
    private readonly IGameHubBus _gameHub;
    private readonly IMediator _mediator;
    private readonly EntityExistsValidator<Data.Player> _playerExists;
    private readonly IPlayerStore _playerStore;
    private readonly IValidatorService _validatorService;

    public UpdatePlayerReadyStateCommandHandler(
        IGameHubBus gameHub,
        IMediator mediator,
        EntityExistsValidator<Data.Player> playerExists,
        IPlayerStore playerStore,
        IValidatorService validatorService)
    {
        _gameHub = gameHub;
        _mediator = mediator;
        _playerExists = playerExists;
        _playerStore = playerStore;
        _validatorService = validatorService;
    }

    public async Task Handle(UpdatePlayerReadyStateCommand request, CancellationToken cancellationToken)
    {
        await _validatorService.Validate(request, _playerExists);

        var player = await _playerStore.Retrieve(request.PlayerId);
        await _playerStore
            .List()
            .Where(p => p.Id == request.PlayerId)
            .ExecuteUpdateAsync(p => p.SetProperty(p => p.IsReady, request.IsReady));

        var syncStartState = await _mediator.Send(new IsSyncStartReadyQuery(player.GameId));
        await _gameHub.SendPlayerReadyStateChanged(syncStartState, request.Actor);
    }
}
