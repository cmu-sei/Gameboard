using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Features.Consoles.Validators;
using Gameboard.Api.Services;
using Gameboard.Api.Structure.MediatR;
using MediatR;

namespace Gameboard.Api.Features.Consoles.Requests;

public record SetActiveUserCommand(ConsoleId Console) : IRequest;

internal sealed class SetActiveUserHandler
(
    IActingUserService actingUserService,
    ICanAccessConsoleValidator canAccessConsole,
    ChallengeService challengeService,
    ConsoleActorMap consoleActorMap,
    IValidatorService validator
) : IRequestHandler<SetActiveUserCommand>
{
    public async Task Handle(SetActiveUserCommand request, CancellationToken cancellationToken)
    {
        canAccessConsole.ChallengeId = request.Console.ChallengeId;

        await validator
            .Auth(c => c.RequireAuthentication())
            .AddEntityExistsValidator<Data.Challenge>(request.Console.ChallengeId)
            .AddValidator(canAccessConsole)
            .Validate(cancellationToken);

        var actingUser = actingUserService.Get();
        consoleActorMap.Update(await challengeService.SetConsoleActor(request.Console, actingUser.Id, actingUser.ApprovedName));
    }
}
