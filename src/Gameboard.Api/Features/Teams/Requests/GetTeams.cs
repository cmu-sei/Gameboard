using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Structure.MediatR.Authorizers;
using MediatR;

namespace Gameboard.Api.Features.Teams;

public record GetTeamsQuery(IEnumerable<string> TeamIds) : IRequest<IEnumerable<Team>>;

internal class GetTeamsRequest : IRequestHandler<GetTeamsQuery, IEnumerable<Team>>
{
    private readonly ITeamService _teamService;
    private readonly UserRoleAuthorizer _userRoleAuthorizer;

    public GetTeamsRequest
    (
        ITeamService teamService,
        UserRoleAuthorizer userRoleAuthorizer
    )
    {
        _teamService = teamService;
        _userRoleAuthorizer = userRoleAuthorizer;
    }

    public async Task<IEnumerable<Team>> Handle(GetTeamsQuery request, CancellationToken cancellationToken)
    {
        _userRoleAuthorizer
            .AllowRoles(UserRole.Admin, UserRole.Director, UserRole.Observer, UserRole.Support, UserRole.Tester)
            .Authorize();

        return await _teamService.GetTeams(request.TeamIds);
    }
}
