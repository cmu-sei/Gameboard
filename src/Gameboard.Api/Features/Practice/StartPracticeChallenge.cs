using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace Gameboard.Api.Features.Practice;

public record StartPracticeChallengeCommand(string ChallengeSpecId, User ActingUser) : IRequest;

internal class StartPracticeChallengeHandler : IRequestHandler<StartPracticeChallengeCommand>
{

    public Task Handle(StartPracticeChallengeCommand request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
