using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace Gameboard.Api.Features.Games;

public record StartGameCommand(string GameId, string TeamId, User ActingUser) : IRequest;

internal class StartGameCommandHandler : IRequestHandler<StartGameCommand>
{
    public Task Handle(StartGameCommand request, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }
}
