using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Features.Teams;
using Gameboard.Api.Structure.MediatR.Authorizers;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Admin;

public record GetAppActiveTeamsQuery() : IRequest<GetAppActiveTeamsResponse>;

internal class GetAppActiveTeamsHandler : IRequestHandler<GetAppActiveTeamsQuery, GetAppActiveTeamsResponse>
{
    private readonly IAppService _appService;
    private readonly ITeamService _teamService;
    private readonly UserRoleAuthorizer _userRoleAuthorizer;

    public GetAppActiveTeamsHandler
    (
        IAppService appService,
        ITeamService teamService,
        UserRoleAuthorizer userRoleAuthorizer
    )
    {
        _appService = appService;
        _teamService = teamService;
        _userRoleAuthorizer = userRoleAuthorizer;
    }

    public async Task<GetAppActiveTeamsResponse> Handle(GetAppActiveTeamsQuery request, CancellationToken cancellationToken)
    {
        // authorize
        _userRoleAuthorizer
            .AllowRoles(UserRole.Admin, UserRole.Director, UserRole.Observer, UserRole.Support, UserRole.Designer)
            .Authorize();

        // pull
        var activeTeamIds = await _appService
            .GetActiveChallenges()
            .Select(c => c.TeamId)
            .Distinct()
            .ToArrayAsync(cancellationToken);

        var captains = await _teamService.ResolveCaptains(activeTeamIds, cancellationToken);
        return new GetAppActiveTeamsResponse(await _teamService.GetTeams(activeTeamIds));
    }
}
