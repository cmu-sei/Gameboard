// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

using Gameboard.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Caching.Distributed;
using Gameboard.Api.Validators;
using Microsoft.AspNetCore.Http;
using System.IO;
using MediatR;
using Gameboard.Api.Features.Sponsors;

namespace Gameboard.Api.Controllers;

[Authorize]
public class SponsorController : _Controller
{
    private readonly IMediator _mediator;
    private readonly ILogger<SponsorController> _logger;
    SponsorService SponsorService { get; }
    public CoreOptions Options { get; }

    public SponsorController(
        IMediator mediator,
        ILogger<SponsorController> logger,
        IDistributedCache cache,
        SponsorValidator validator,
        SponsorService sponsorService,
        CoreOptions options
    ) : base(logger, cache, validator)
    {
        _mediator = mediator;
        _logger = logger;
        SponsorService = sponsorService;
        Options = options;
    }

    /// <summary>
    /// Create new sponsor
    /// </summary>
    /// <remarks>
    /// We have to read these values from the form rather than the body because you can only do one at a time in
    /// .NET Core, and you can't bind IFormFile form the body
    /// </remarks>
    /// <param name="name">The display name of the sponsor entity.</param>
    /// <param name="logoFile">An image file blob.</param>
    /// <returns>Sponsor</returns>
    [HttpPost("api/sponsor")]
    [Authorize(Policy = AppConstants.RegistrarPolicy)]
    public Task<Sponsor> Create([FromForm] string name, [FromForm] IFormFile logoFile)
        => _mediator.Send(new CreateSponsorRequest(new NewSponsor { LogoFile = logoFile, Name = name }, Actor));

    [HttpPost("api/sponsors")]
    [Authorize(Policy = AppConstants.RegistrarPolicy)]
    public async Task CreateBatch([FromBody] ChangedSponsor[] model)
    {
        foreach (var s in model)
        {
            await SponsorService.AddOrUpdate(s);
        }
    }

    /// <summary>
    /// Retrieve sponsor
    /// </summary>
    /// <param name="id"></param>
    /// <returns>Sponsor</returns>
    [HttpGet("api/sponsor/{id}")]
    [Authorize]
    public async Task<Sponsor> Retrieve([FromRoute] string id)
    {
        return await SponsorService.Retrieve(id);
    }

    /// <summary>
    /// Change sponsor
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    [HttpPut("api/sponsor")]
    [Authorize(Policy = AppConstants.RegistrarPolicy)]
    public async Task Update([FromBody] ChangedSponsor model)
    {
        await Validate(model);
        await SponsorService.AddOrUpdate(model);
    }

    /// <summary>
    /// Delete sponsor
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [HttpDelete("/api/sponsor/{id}")]
    [Authorize(Policy = AppConstants.RegistrarPolicy)]
    public async Task Delete([FromRoute] string id)
    {
        await SponsorService.Delete(id);
    }

    /// <summary>
    /// Find sponsors
    /// </summary>
    /// <param name="model">DataFilter</param>
    /// <returns>Sponsor[]</returns>
    [HttpGet("/api/sponsors")]
    [Authorize]
    public async Task<Sponsor[]> List([FromQuery] SearchFilter model)
    {
        return await SponsorService.List(model);
    }
}
