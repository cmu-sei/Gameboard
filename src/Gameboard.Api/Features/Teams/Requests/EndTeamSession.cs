using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Data;
using Gameboard.Api.Structure.MediatR;
using Gameboard.Api.Structure.MediatR.Validators;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Teams;

public record EndTeamSessionCommand(string TeamId, User Actor) : IRequest;

internal class EndTeamSessionHandler : IRequestHandler<EndTeamSessionCommand>
{
    private readonly IStore _store;
    private readonly TeamExistsValidator<EndTeamSessionCommand> _teamExists;
    private readonly ITeamService _teamService;
    private readonly IValidatorService<EndTeamSessionCommand> _validatorService;

    public EndTeamSessionHandler
    (
        IStore store,
        TeamExistsValidator<EndTeamSessionCommand> teamExists,
        ITeamService teamService,
        IValidatorService<EndTeamSessionCommand> validatorService
    )
    {
        _store = store;
        _teamExists = teamExists;
        _teamService = teamService;
        _validatorService = validatorService;
    }

    public async Task Handle(EndTeamSessionCommand request, CancellationToken cancellationToken)
    {
        // validate 
        var players = await _store
            .WithNoTracking<Data.Player>()
            .Where(p => p.TeamId == request.TeamId)
            .ToArrayAsync(cancellationToken);

        _validatorService.AddValidator(_teamExists.UseProperty(r => r.TeamId));
        _validatorService.AddValidator((req, context) =>
        {
            if (!request.Actor.IsRegistrar && !players.Any(p => p.UserId == req.Actor.Id))
                throw new ActionForbidden();
        });

        // end session
        await _teamService.EndSession(request.TeamId, request.Actor, cancellationToken);
    }
}
