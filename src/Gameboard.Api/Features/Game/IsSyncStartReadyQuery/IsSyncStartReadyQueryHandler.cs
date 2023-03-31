using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Data.Abstractions;
using Gameboard.Api.Services;
using Gameboard.Api.Structure.MediatR;
using Gameboard.Api.Structure.MediatR.Validators;
using MediatR;

namespace Gameboard.Api.Features.Games;

public record IsSyncStartReadyQuery(string gameId) : IRequest<SyncStartState>;

internal class IsSyncStartReadyQueryHandler : IRequestHandler<IsSyncStartReadyQuery, SyncStartState>
{
    private readonly EntityExistsValidator<IsSyncStartReadyQuery, Data.Game> _gameExists;
    private readonly IGameService _gameService;
    private readonly IPlayerStore _playerStore;
    private readonly IValidatorService<IsSyncStartReadyQuery> _validatorService;

    public IsSyncStartReadyQueryHandler
    (
        EntityExistsValidator<IsSyncStartReadyQuery, Data.Game> gameExists,
        IGameService gameService,
        IPlayerStore playerStore,
        IValidatorService<IsSyncStartReadyQuery> validatorService
    )
    {
        _gameExists = gameExists;
        _gameService = gameService;
        _playerStore = playerStore;
        _validatorService = validatorService;
    }

    public async Task<SyncStartState> Handle(IsSyncStartReadyQuery request, CancellationToken cancellationToken)
    {
        _validatorService.AddValidator(_gameExists.UseProperty(r => r.gameId));
        await _validatorService.Validate(request);

        return await _gameService.GetSyncStartState(request.gameId);
    }
}
