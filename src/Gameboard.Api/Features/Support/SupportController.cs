// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Gameboard.Api.Data;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gameboard.Api.Features.Support;

[ApiController]
[Authorize]
[Route("api/support")]
public class SupportController(IMediator mediator) : ControllerBase
{
    private readonly IMediator _mediator = mediator;

    [HttpGet("settings")]
    public Task<SupportSettingsViewModel> GetSupportSettingsGreeting()
        => _mediator.Send(new GetSupportSettingsQuery());

    [HttpPut("settings")]
    public Task<SupportSettingsViewModel> UpdateSupportSettings([FromBody] SupportSettingsViewModel settings)
        => _mediator.Send(new UpdateSupportSettingsCommand(settings));

    [HttpDelete("settings/autotag/{id}")]
    public Task DeleteAutoTag([FromRoute] string id)
        => _mediator.Send(new DeleteSupportSettingsAutoTagCommand(id));

    [HttpGet("settings/autotags")]
    public Task<IEnumerable<SupportSettingsAutoTagViewModel>> GetAutoTags()
        => _mediator.Send(new GetSupportSettingsAutoTagsQuery());

    [HttpPost("settings/autotag")]
    public Task<SupportSettingsAutoTag> UpsertAutoTag([FromBody] UpsertSupportSettingsAutoTagRequest request)
        => _mediator.Send(new UpsertSupportSettingsAutoTagCommand(request));
}
