using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Features.App;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gameboard.Api.Features.Settings;

[Authorize]
[Route("api/settings")]
public class SettingsController(IMediator mediator) : ControllerBase
{
    private readonly IMediator _mediator = mediator;

    [HttpGet]
    public Task<GetSettingsResponse> Get(CancellationToken cancellationToken)
        => _mediator.Send(new GetSettingsQuery(), cancellationToken);
}
