// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gameboard.Api.Features.Teams;

[ApiController]
[Route("api/admin/team")]
[Authorize]
public class AdminTeamsController(IMediator mediator) : ControllerBase
{
    private readonly IMediator _mediator = mediator;

    [HttpPost]
    public Task<AdminEnrollTeamResponse> Create([FromBody] AdminEnrollTeamRequest request)
        => _mediator.Send(request);

    [HttpGet("search")]
    public Task<IEnumerable<Team>> SearchTeams([FromQuery] string ids)
        => _mediator.Send(new GetTeamsQuery(ids.Split(",", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)));

    [HttpPut("session")]
    public Task<AdminExtendTeamSessionResponse> ExtendTeamSessions([FromBody] AdminExtendTeamSessionRequest request)
        => _mediator.Send(request);
}
