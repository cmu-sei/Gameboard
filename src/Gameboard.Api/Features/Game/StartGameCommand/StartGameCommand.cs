using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Services;
using MediatR;

namespace Gameboard.Api.Features.Games;

public record StartGameCommand(string GameId, string TeamId, User ActingUser) : IRequest;

internal class StartGameCommandHandler : IRequestHandler<StartGameCommand>
{
    private readonly IGameService _gameService;

    public StartGameCommandHandler(IGameService gameService)
    {
        _gameService = gameService;
    }

    public async Task Handle(StartGameCommand request, CancellationToken cancellationToken)
    {
        var game = await _gameService.Retrieve(request.GameId);

        throw new System.NotImplementedException();
    }
}
