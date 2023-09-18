using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Services;
using MediatR;

namespace Gameboard.Api.Features.Games;

public record GetSyncStartStateQuery(string gameId, User ActingUser) : IRequest<SyncStartState>;

internal class GetSyncStartStateQueryHandler : IRequestHandler<GetSyncStartStateQuery, SyncStartState>
{
    private readonly IGameService _gameService;
    private readonly GetSyncStartStateQueryValidator _validator;

    public GetSyncStartStateQueryHandler
    (
        IGameService gameService,
        GetSyncStartStateQueryValidator validator
    )
    {
        _gameService = gameService;
        _validator = validator;
    }

    public async Task<SyncStartState> Handle(GetSyncStartStateQuery request, CancellationToken cancellationToken)
    {
        await _validator.Validate(request);
        return await _gameService.GetSyncStartState(request.gameId);
    }
}
