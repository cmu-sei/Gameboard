// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Features.Users;
using Gameboard.Api.Structure.MediatR;
using MediatR;

namespace Gameboard.Api.Features.Teams;

public record GetTeamsQuery(IEnumerable<string> TeamIds) : IRequest<IEnumerable<Team>>;

internal class GetTeamsRequest(
    ITeamService teamService,
    IValidatorService validatorService
    ) : IRequestHandler<GetTeamsQuery, IEnumerable<Team>>
{
    private readonly ITeamService _teamService = teamService;
    private readonly IValidatorService _validatorService = validatorService;

    public async Task<IEnumerable<Team>> Handle(GetTeamsQuery request, CancellationToken cancellationToken)
    {
        await _validatorService
            .Auth(a => a.Require(PermissionKey.Admin_View))
            .Validate(cancellationToken);

        return await _teamService.GetTeams(request.TeamIds);
    }
}
