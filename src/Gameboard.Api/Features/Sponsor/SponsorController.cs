// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using Gameboard.Api.Features.Sponsors;
using Gameboard.Api.Services;
using Gameboard.Api.Common.Services;

namespace Gameboard.Api.Controllers;

[Authorize]
public class SponsorController
{
    private readonly IActingUserService _actingUserService;
    private readonly IMediator _mediator;
    private readonly SponsorService _sponsorService;

    public SponsorController
    (
        IActingUserService actingUserService,
        IMediator mediator,
        SponsorService sponsorService
    )
    {
        _actingUserService = actingUserService;
        _mediator = mediator;
        _sponsorService = sponsorService;
    }

    /// <summary>
    /// Find sponsors
    /// </summary>
    /// <param name="model">DataFilter</param>
    /// <returns>Sponsor[]</returns>
    [HttpGet("/api/sponsors")]
    [Authorize]
    public Task<Sponsor[]> List([FromQuery] SponsorSearch model)
        => _sponsorService.List(model);

    /// <summary>
    /// Find sponsors
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Sponsor[]</returns>
    [HttpGet("/api/sponsors/with-children")]
    [Authorize]
    public Task<IEnumerable<SponsorWithChildSponsors>> ListWithChildren(CancellationToken cancellationToken)
        => _mediator.Send(new GetSponsorsWithChildrenQuery(), cancellationToken);

    /// <summary>
    /// Create new sponsor
    /// </summary>
    /// <remarks>
    /// We have to read these values from the form rather than the body because you can only do one at a time in
    /// .NET Core, and you can't bind IFormFile form the body
    /// </remarks>
    /// <param name="newSponsor">A requets containing the new sponsor's name and (optionally) its parentSponsorId.</param>
    /// <returns>Sponsor</returns>
    [HttpPost("api/sponsor")]
    [Authorize(Policy = AppConstants.RegistrarPolicy)]
    public Task<SponsorWithParentSponsor> Create([FromBody] NewSponsor newSponsor)
        => _mediator.Send(new CreateSponsorCommand(newSponsor, _actingUserService.Get()));

    /// <summary>
    /// Add multiple sponsors to the application in a batch.
    /// </summary>
    /// <remarks>
    /// This endpoint currently only allows the addition of sponsors by name - logo files and parent sponsors must be managed
    /// via the UI (or uploaded one by one using the api/sponsor endpoint). The web client doesn't currently use this endpoint.
    /// </remarks>
    /// <param name="model"></param>
    /// <returns></returns>
    [HttpPost("api/sponsors")]
    [Authorize(Policy = AppConstants.RegistrarPolicy)]
    public async Task CreateBatch([FromBody] UpdateSponsorRequest[] model)
    {
        foreach (var s in model)
            await _sponsorService.AddOrUpdate(s);
    }

    /// <summary>
    /// Retrieve sponsor
    /// </summary>
    /// <param name="id"></param>
    /// <returns>Sponsor</returns>
    [HttpGet("api/sponsor/{id}")]
    [Authorize]
    public Task<Sponsor> Retrieve([FromRoute] string id)
        => _sponsorService.Retrieve(id);

    /// <summary>
    /// Update sponsor
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    [HttpPut("api/sponsor")]
    [Authorize(Policy = AppConstants.RegistrarPolicy)]
    public Task<Sponsor> Update([FromBody] UpdateSponsorRequest model)
        => _mediator.Send(new UpdateSponsorCommand(model, _actingUserService.Get()));

    [HttpPut("api/sponsor/{sponsorId}/avatar")]
    [Authorize(Policy = AppConstants.RegistrarPolicy)]
    public Task UpdateSponsorAvatar([FromRoute] string sponsorId, [FromForm] IFormFile avatarFile, CancellationToken cancellationToken)
        => _mediator.Send(new SetSponsorAvatarCommand(sponsorId, avatarFile, _actingUserService.Get()), cancellationToken);

    /// <summary>
    /// Delete sponsor
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [HttpDelete("/api/sponsor/{id}")]
    [Authorize(Policy = AppConstants.RegistrarPolicy)]
    public Task Delete([FromRoute] string id)
        => _mediator.Send(new DeleteSponsorCommand(id, _actingUserService.Get()));
}
