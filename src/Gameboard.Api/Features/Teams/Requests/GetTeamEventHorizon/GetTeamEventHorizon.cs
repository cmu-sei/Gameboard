using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Data;
using Gameboard.Api.Structure.MediatR;
using Gameboard.Api.Structure.MediatR.Authorizers;
using Gameboard.Api.Structure.MediatR.Validators;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Teams;

public record GetTeamEventHorizonQuery(string TeamId) : IRequest<EventHorizon>;

internal class GetTeamEventHorizonHandler : IRequestHandler<GetTeamEventHorizonQuery, EventHorizon>
{
    private readonly IActingUserService _actingUserService;
    private readonly IStore _store;
    private readonly TeamExistsValidator<GetTeamEventHorizonQuery> _teamExists;
    private readonly UserRoleAuthorizer _userRoleAuthorizer;
    private readonly IValidatorService<GetTeamEventHorizonQuery> _validator;

    public GetTeamEventHorizonHandler
    (
        IActingUserService actingUserService,
        IStore store,
        TeamExistsValidator<GetTeamEventHorizonQuery> teamExists,
        UserRoleAuthorizer userRoleAuthorizer,
        IValidatorService<GetTeamEventHorizonQuery> validator
    )
    {
        _actingUserService = actingUserService;
        _store = store;
        _teamExists = teamExists;
        _userRoleAuthorizer = userRoleAuthorizer;
        _validator = validator;
    }

    public async Task<EventHorizon> Handle(GetTeamEventHorizonQuery request, CancellationToken cancellationToken)
    {
        // validate
        var actingUserId = _actingUserService.Get().Id;

        await _validator
            .AddValidator(_teamExists.UseProperty(r => r.TeamId))
            .AddValidator(async (req, ctx) =>
            {
                // people with elevated roles can always see this, but regular players can't
                // unless they're on the team
                _userRoleAuthorizer.AllowRoles(UserRole.Admin, UserRole.Support, UserRole.Designer);
                if (!_userRoleAuthorizer.WouldAuthorize())
                {
                    var isUserOnTeam = await _store
                        .WithNoTracking<Data.Player>()
                        .Where(p => p.TeamId == req.TeamId && p.UserId == actingUserId)
                        .AnyAsync();

                    if (!isUserOnTeam)
                        ctx.AddValidationException(new UserIsntOnTeam(actingUserId, req.TeamId));
                }
            })
            .Validate(request, cancellationToken);

        // and awaaaaay we go

    }
}
