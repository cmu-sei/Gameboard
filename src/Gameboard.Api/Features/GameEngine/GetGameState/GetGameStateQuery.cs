using MediatR;

namespace Gameboard.Api.Features.GameEngine.Requests;

public record GetGameStateQuery(string TeamId) : IRequest<GameEngineGameState>;
