using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Structure.MediatR;
using MediatR;

namespace Gameboard.Api.Features.Games;

public record GetSyncStartStateQuery(string GameId, User ActingUser) : IRequest<SyncStartState>;

internal class GetSyncStartStateQueryHandler : IRequestHandler<GetSyncStartStateQuery, SyncStartState>
{
    private readonly ISyncStartGameService _syncStartGameService;
    private readonly IGameboardRequestValidator<GetSyncStartStateQuery> _validator;

    public GetSyncStartStateQueryHandler
    (
        ISyncStartGameService syncStartGameService,
        IGameboardRequestValidator<GetSyncStartStateQuery> validator
    )
    {
        _syncStartGameService = syncStartGameService;
        _validator = validator;
    }

    public async Task<SyncStartState> Handle(GetSyncStartStateQuery request, CancellationToken cancellationToken)
    {
        await _validator.Validate(request, cancellationToken);
        // TODO: make sync start service use teamId instead of gameid
        return await _syncStartGameService.GetSyncStartState(request.GameId, null, cancellationToken);
    }
}
