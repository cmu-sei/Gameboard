using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace Gameboard.Api.Features.Games;

public record GetSyncStartStateQuery(string gameId, User ActingUser) : IRequest<SyncStartState>;

internal class GetSyncStartStateQueryHandler : IRequestHandler<GetSyncStartStateQuery, SyncStartState>
{
    private readonly ISyncStartGameService _syncStartGameService;
    private readonly GetSyncStartStateQueryValidator _validator;

    public GetSyncStartStateQueryHandler
    (
        ISyncStartGameService syncStartGameService,
        GetSyncStartStateQueryValidator validator
    )
    {
        _syncStartGameService = syncStartGameService;
        _validator = validator;
    }

    public async Task<SyncStartState> Handle(GetSyncStartStateQuery request, CancellationToken cancellationToken)
    {
        await _validator.Validate(request);
        return await _syncStartGameService.GetSyncStartState(request.gameId);
    }
}
