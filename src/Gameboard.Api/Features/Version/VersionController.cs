using System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gameboard.Api.Features.Version;

public class VersionController : Controller
{
    /// <summary>
    /// check version
    /// </summary>
    /// <returns>The commit SHA of the current application version.</returns>
    [HttpGet("/api/version")]
    public IActionResult Version()
    {
        return Ok(new
        {
            Commit = Environment.GetEnvironmentVariable("COMMIT") ?? "no version info"
        });
    }
}
