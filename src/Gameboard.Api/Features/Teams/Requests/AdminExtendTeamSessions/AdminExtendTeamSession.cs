using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Structure.MediatR.Authorizers;
using MediatR;

namespace Gameboard.Api.Features.Teams;

public record AdminExtendTeamSessionRequest(IEnumerable<string> TeamIds, double ExtensionDurationInMinutes) : IRequest<AdminExtendTeamSessionResponse>;
public record AdminExtendTeamSessionResponse(IEnumerable<AdminExtendTeamSessionResponseTeam> Teams);
public record AdminExtendTeamSessionResponseTeam(string Id, DateTimeOffset SessionEnd);

internal class AdminExtendTeamSessionHandler : IRequestHandler<AdminExtendTeamSessionRequest, AdminExtendTeamSessionResponse>
{
    private readonly IActingUserService _actingUserService;
    private readonly ITeamService _teamService;
    private readonly UserRoleAuthorizer _userRoleAuthorizer;

    public AdminExtendTeamSessionHandler
    (
        IActingUserService actingUserService,
        ITeamService teamService,
        UserRoleAuthorizer userRoleAuthorizer
    )
    {
        _actingUserService = actingUserService;
        _teamService = teamService;
        _userRoleAuthorizer = userRoleAuthorizer;
    }

    public async Task<AdminExtendTeamSessionResponse> Handle(AdminExtendTeamSessionRequest request, CancellationToken cancellationToken)
    {
        _userRoleAuthorizer
            .AllowRoles(UserRole.Admin, UserRole.Designer, UserRole.Director, UserRole.Observer, UserRole.Registrar, UserRole.Support)
            .Authorize();

        var teams = await _teamService.GetTeams(request.TeamIds);
        var captains = new List<Api.Player>();
        foreach (var team in teams)
        {
            captains.Add(await _teamService.ExtendSession(new ExtendTeamSessionRequest
            {
                Actor = _actingUserService.Get(),
                NewSessionEnd = team.SessionEnd.ToUniversalTime().AddMinutes(request.ExtensionDurationInMinutes),
                TeamId = team.TeamId
            }, cancellationToken));
        };

        return new AdminExtendTeamSessionResponse
        (
            captains.Select(p => new AdminExtendTeamSessionResponseTeam(p.TeamId, p.SessionEnd.ToUniversalTime()))
        );
    }
}
