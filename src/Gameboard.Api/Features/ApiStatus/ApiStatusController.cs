using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gameboard.Api.Features.Status;

[ApiController]
[Route("/api/status")]
public class ApiStatusController : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public ActionResult Get()
        => Ok();
}
