using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Consoles.Validators;
using Gameboard.Api.Features.GameEngine;
using Gameboard.Api.Structure.MediatR;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Consoles.Requests;

public record GetConsoleRequest(string ChallengeId, string Name) : IRequest<ConsoleState>;

internal sealed class GetConsoleHandler
(
    ICanAccessConsoleValidator canAccessConsoleValidator,
    IGameEngineService gameEngine,
    IStore store,
    IValidatorService validatorService
) : IRequestHandler<GetConsoleRequest, ConsoleState>
{
    public async Task<ConsoleState> Handle(GetConsoleRequest request, CancellationToken cancellationToken)
    {
        // configure console access validator
        canAccessConsoleValidator.ChallengeId = request.ChallengeId;

        // validate
        await validatorService
            .AddEntityExistsValidator<Data.Challenge>(request.ChallengeId)
            .AddValidator(canAccessConsoleValidator)
            .Validate(cancellationToken);

        // get the console and its state
        var challenge = await store
            .WithNoTracking<Data.Challenge>()
            .Select(c => new { c.Id, c.State, c.GameEngineType })
            .SingleAsync(c => c.Id == request.ChallengeId, cancellationToken);
        var state = await gameEngine.GetChallengeState(GameEngineType.TopoMojo, challenge.State);

        if (!state.Vms.Any(v => v.Name == request.Name))
        {
            var vmNames = string.Join(", ", state.Vms.Select(vm => vm.Name));
            throw new ResourceNotFound<GameEngineVmState>("n/a", $"VMS for challenge {request.ChallengeId} - searching for {request.Name}, found these names: {vmNames}");
        }

        var console = await gameEngine.GetConsole(challenge.GameEngineType, new ConsoleId() { ChallengeId = request.ChallengeId, Name = request.Name }, cancellationToken);
        return console ?? throw new InvalidConsoleAction();
    }
}
