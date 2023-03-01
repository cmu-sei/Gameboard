using MediatR;

namespace Gameboard.Api.Features.GameEngine;

public class GetGameStateRequest : IRequest<GameEngineGameState>
{
    public string TeamId { get; private set; }

    public GetGameStateRequest(string teamId)
    {
        TeamId = teamId;
    }
}
