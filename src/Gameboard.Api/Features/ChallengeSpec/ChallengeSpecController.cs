// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Threading.Tasks;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Features.ChallengeSpecs;
using Gameboard.Api.Features.Users;
using Gameboard.Api.Services;
using Gameboard.Api.Validators;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace Gameboard.Api.Controllers;

[Authorize]
public class ChallengeSpecController(
        IActingUserService actingUserService,
        ILogger<ChallengeSpecController> logger,
        IDistributedCache cache,
        IMediator mediator,
        ChallengeSpecValidator validator,
        ChallengeSpecService challengespecService,
        IUserRolePermissionsService permissionsService
        ) : _Controller(actingUserService, logger, cache, validator)
{
    private readonly IMediator _mediator = mediator;
    private readonly IUserRolePermissionsService _permissionsService = permissionsService;
    ChallengeSpecService ChallengeSpecService { get; } = challengespecService;

    /// <summary>
    /// Create a new challengespec.
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    [HttpPost("api/challengespec")]
    public async Task<ChallengeSpec> Create([FromBody] NewChallengeSpec model)
    {
        await AuthorizeAny(_permissionsService.Can(PermissionKey.Games_CreateEditDelete));
        await Validate(model);

        return await ChallengeSpecService.AddOrUpdate(model);
    }

    /// <summary>
    /// Retrieve challengespec
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [HttpGet("api/challengespec/{id}")]
    public async Task<ChallengeSpec> Retrieve([FromRoute] string id)
    {
        return await ChallengeSpecService.Retrieve(id);
    }

    /// <summary>
    /// Change challengespec
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    [HttpPut("api/challengespec")]
    public async Task Update([FromBody] ChangedChallengeSpec model)
    {
        await AuthorizeAny(_permissionsService.Can(PermissionKey.Games_CreateEditDelete));
        await ChallengeSpecService.Update(model);
    }

    /// <summary>
    /// Delete challengespec
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [HttpDelete("/api/challengespec/{id}")]
    public async Task Delete([FromRoute] string id)
    {
        await AuthorizeAny(_permissionsService.Can(PermissionKey.Games_CreateEditDelete));
        await ChallengeSpecService.Delete(id);
    }

    /// <summary>
    /// Find challengespecs
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    [HttpGet("/api/challengespecs")]
    public async Task<ExternalSpec[]> List([FromQuery] SearchFilter model)
    {
        await AuthorizeAny(_permissionsService.Can(PermissionKey.Games_CreateEditDelete));
        return await ChallengeSpecService.List(model);
    }

    /// <summary>
    /// Load solve performance for the challenge spec
    /// </summary>
    [HttpGet("/api/challengespecs/{challengeSpecId}/question-performance")]
    public Task<GetChallengeSpecQuestionPerformanceResult> GetQuestionPerformance([FromRoute] string challengeSpecId)
        => _mediator.Send(new GetChallengeSpecQuestionPerformanceQuery(challengeSpecId));

    /// <summary>
    /// Sync challengespec name/description with external source
    /// </summary>
    /// <param name="id">game id</param>
    /// <returns></returns>
    [HttpPost("/api/challengespecs/sync/{id}")]
    public async Task Sync([FromRoute] string id)
    {
        await AuthorizeAny(_permissionsService.Can(PermissionKey.Games_CreateEditDelete));
        await ChallengeSpecService.Sync(id);
    }
}
