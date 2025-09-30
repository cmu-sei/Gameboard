// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Features.Teams;
using Gameboard.Api.Structure.MediatR;
using Gameboard.Api.Structure.MediatR.Validators;

namespace Gameboard.Api.Features.GameEngine;

internal class GetGameStateValidator(
    IActingUserService actingUserService,
    TeamExistsValidator<GetGameStateQuery> teamExists,
    ITeamService teamService,
    IValidatorService<GetGameStateQuery> validatorService
    ) : IGameboardRequestValidator<GetGameStateQuery>
{
    private readonly User _actingUser = actingUserService.Get();
    private readonly TeamExistsValidator<GetGameStateQuery> _teamExists = teamExists;
    private readonly ITeamService _teamService = teamService;
    private readonly IValidatorService<GetGameStateQuery> _validatorService = validatorService;

    public async Task Validate(GetGameStateQuery request, CancellationToken cancellationToken)
    {

        await _validatorService
            .Auth
            (
                a => a
                    .Require(Users.PermissionKey.Admin_View)
                    .Unless
                    (
                        () => _teamService.IsOnTeam(request.TeamId, _actingUser.Id),
                        new PlayerIsntOnTeam(_actingUser.Id, request.TeamId, "[unknown]")
                    )
            )
            .AddValidator(_teamExists.UseProperty(r => r.TeamId))
            .Validate(request, cancellationToken);
    }
}
