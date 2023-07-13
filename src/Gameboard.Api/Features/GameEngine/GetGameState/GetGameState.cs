using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Structure.MediatR;
using MediatR;

namespace Gameboard.Api.Features.GameEngine;

public record GetGameStateQuery(string TeamId) : IRequest<IEnumerable<GameEngineGameState>>;

internal class GetGameStateHandler : IRequestHandler<GetGameStateQuery, IEnumerable<GameEngineGameState>>
{
    private readonly IGameEngineStore _gameEngineStore;
    private readonly IGameboardRequestValidator<GetGameStateQuery> _validator;

    public GetGameStateHandler
    (
        IGameEngineStore gameEngineStore,
        IGameboardRequestValidator<GetGameStateQuery> validator
    )
    {
        _gameEngineStore = gameEngineStore;
        _validator = validator;
    }

    public async Task<IEnumerable<GameEngineGameState>> Handle(GetGameStateQuery request, CancellationToken cancellationToken)
    {
        await _validator.Validate(request);
        return await _gameEngineStore.GetGameStatesByTeam(request.TeamId);
    }
}
