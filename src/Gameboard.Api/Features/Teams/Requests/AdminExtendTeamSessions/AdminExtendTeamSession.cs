using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Structure.MediatR;
using MediatR;

namespace Gameboard.Api.Features.Teams;

public record AdminExtendTeamSessionRequest(IEnumerable<string> TeamIds, double ExtensionDurationInMinutes) : IRequest<AdminExtendTeamSessionResponse>;
public record AdminExtendTeamSessionResponse(IEnumerable<AdminExtendTeamSessionResponseTeam> Teams);
public record AdminExtendTeamSessionResponseTeam(string Id, DateTimeOffset SessionEnd);

internal class AdminExtendTeamSessionHandler(
    IActingUserService actingUserService,
    ITeamService teamService,
    IValidatorService validatorService
    ) : IRequestHandler<AdminExtendTeamSessionRequest, AdminExtendTeamSessionResponse>
{
    private readonly IActingUserService _actingUserService = actingUserService;
    private readonly ITeamService _teamService = teamService;
    private readonly IValidatorService _validatorService = validatorService;

    public async Task<AdminExtendTeamSessionResponse> Handle(AdminExtendTeamSessionRequest request, CancellationToken cancellationToken)
    {
        await _validatorService
            .ConfigureAuthorization(c => c.RequirePermissions(Users.PermissionKey.Teams_EditSession))
            .Validate(cancellationToken);

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
