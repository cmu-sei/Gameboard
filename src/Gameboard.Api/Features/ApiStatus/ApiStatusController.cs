// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

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
