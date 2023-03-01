using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Structure;
using MediatR;

namespace Gameboard.Api.Features.GameEngine;

// public record GetGameStateQuery(string teamId) : IRequest<GameEngineGameState>;

public class GetGameStateHandler : IRequestHandler<GetGameStateRequest, GameEngineGameState>
{
    private readonly IGameEngineStore _gameEngineStore;

    public GetGameStateHandler(IGameEngineStore gameEngineStore)
    {
        _gameEngineStore = gameEngineStore;
    }

    public Task<GameEngineGameState> Handle(GetGameStateRequest request, CancellationToken cancellationToken)
        => _gameEngineStore.GetGameStateByTeam(request.TeamId);
}
