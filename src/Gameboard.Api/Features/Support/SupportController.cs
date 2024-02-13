using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gameboard.Api.Features.Support;

[Authorize]
[Route("api/support")]
public class SupportController : ControllerBase
{
    private readonly IMediator _mediator;

    public SupportController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("settings")]
    public Task<SupportSettingsViewModel> GetSupportSettingsGreeting()
        => _mediator.Send(new GetSupportSettingsQuery());

    [HttpPut("settings")]
    public Task<SupportSettingsViewModel> UpdateSupportSettings([FromBody] SupportSettingsViewModel settings)
        => _mediator.Send(new UpdateSupportSettingsCommand(settings));
}
