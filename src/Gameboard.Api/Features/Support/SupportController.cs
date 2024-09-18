using System.Collections.Generic;
using System.Threading.Tasks;
using Gameboard.Api.Data;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gameboard.Api.Features.Support;

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

    [HttpGet("settings/autotags")]
    public Task<IEnumerable<SupportSettingsAutoTagViewModel>> GetAutoTags()
        => _mediator.Send(new GetSupportSettingsAutoTagsQuery());

    [HttpPost("settings/autotag")]
    public Task<SupportSettingsAutoTag> UpsertAutoTag([FromBody] UpsertSupportSettingsAutoTagRequest request)
        => _mediator.Send(new UpsertSupportSettingsAutoTagCommand(request));
}
