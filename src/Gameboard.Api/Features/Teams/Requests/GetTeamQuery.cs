// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Structure.MediatR;
using Gameboard.Api.Structure.MediatR.Validators;
using MediatR;

namespace Gameboard.Api.Features.Teams;

public record GetTeamQuery(string TeamId, User User) : IRequest<Team>;

internal class GetTeamQueryHandler(
    TeamExistsValidator<GetTeamQuery> teamExists,
    ITeamService teamService,
    IValidatorService<GetTeamQuery> validatorService) : IRequestHandler<GetTeamQuery, Team>
{
    private readonly TeamExistsValidator<GetTeamQuery> _teamExists = teamExists;
    private readonly ITeamService _teamService = teamService;
    private readonly IValidatorService<GetTeamQuery> _validatorService = validatorService;

    public async Task<Team> Handle(GetTeamQuery request, CancellationToken cancellationToken)
    {
        _validatorService.AddValidator(_teamExists.UseProperty(r => r.TeamId));
        await _validatorService.Validate(request, cancellationToken);

        return await _teamService.GetTeam(request.TeamId);
    }
}
