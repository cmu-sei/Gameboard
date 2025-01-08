using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Users;
using Gameboard.Api.Structure.MediatR;
using Gameboard.Api.Structure.MediatR.Validators;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Teams;

public record EndTeamSessionCommand(string TeamId, User Actor) : IRequest<TeamSessionUpdate>;

internal class EndTeamSessionHandler
(
    INowService nowService,
    IUserRolePermissionsService permissionsService,
    IStore store,
    TeamExistsValidator<EndTeamSessionCommand> teamExists,
    ITeamService teamService,
    IValidatorService<EndTeamSessionCommand> validatorService
) : IRequestHandler<EndTeamSessionCommand, TeamSessionUpdate>
{
    private readonly INowService _nowService = nowService;
    private readonly IUserRolePermissionsService _permissionsService = permissionsService;
    private readonly IStore _store = store;
    private readonly TeamExistsValidator<EndTeamSessionCommand> _teamExists = teamExists;
    private readonly ITeamService _teamService = teamService;
    private readonly IValidatorService<EndTeamSessionCommand> _validatorService = validatorService;

    public async Task<TeamSessionUpdate> Handle(EndTeamSessionCommand request, CancellationToken cancellationToken)
    {
        // validate 
        var players = await _store
            .WithNoTracking<Data.Player>()
            .Where(p => p.TeamId == request.TeamId)
            .ToArrayAsync(cancellationToken);

        _validatorService.AddValidator(_teamExists.UseProperty(r => r.TeamId));
        _validatorService.AddValidator(async (req, context) =>
        {
            var canManageSessions = await _permissionsService.Can(PermissionKey.Teams_EditSession);

            if (!canManageSessions && !players.Any(p => p.UserId == req.Actor.Id))
                throw new ActionForbidden();
        });

        // end session
        var nowish = _nowService.Get();
        await _teamService.EndSession(request.TeamId, request.Actor, cancellationToken);
        return new TeamSessionUpdate { Id = request.TeamId, SessionEndsAt = nowish.ToUnixTimeMilliseconds() };
    }
}
