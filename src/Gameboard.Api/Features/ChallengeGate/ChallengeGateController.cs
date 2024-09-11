// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Caching.Distributed;
using Gameboard.Api.ChallengeGates;
using Gameboard.Api.Features.Users;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Services;

namespace Gameboard.Api.Controllers;

[Authorize]
public class ChallengeGateController(
        IActingUserService actingUserService,
        ILogger<ChallengeGateController> logger,
        IDistributedCache cache,
        ChallengeGateValidator validator,
        ChallengeGateService challengeGateService,
        IUserRolePermissionsService permissionsService
        ) : GameboardLegacyController(actingUserService, logger, cache, validator)
{
    ChallengeGateService ChallengeGateService { get; } = challengeGateService;

    private readonly IUserRolePermissionsService _permissionsService = permissionsService;

    /// <summary>
    /// Create new challenge gate
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    [HttpPost("api/challengegate")]
    public async Task<ChallengeGate> Create([FromBody] NewChallengeGate model)
    {
        if (!await _permissionsService.Can(PermissionKey.Games_CreateEditDelete))
            throw new ActionForbidden();

        await Validate(model);

        return await ChallengeGateService.AddOrUpdate(model);
    }

    /// <summary>
    /// Retrieve challenge gate
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [HttpGet("api/challengegate/{id}")]
    public async Task<ChallengeGate> Retrieve([FromRoute] string id)
    {
        if (!await _permissionsService.Can(PermissionKey.Games_CreateEditDelete))
            throw new ActionForbidden();

        return await ChallengeGateService.Retrieve(id);
    }

    /// <summary>
    /// Change challenge gate
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    [HttpPut("api/challengegate")]
    public async Task Update([FromBody] ChangedChallengeGate model)
    {
        if (!await _permissionsService.Can(PermissionKey.Games_CreateEditDelete))
            throw new ActionForbidden();

        await ChallengeGateService.Update(model);
        return;
    }

    /// <summary>
    /// Delete challenge gate
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [HttpDelete("/api/challengegate/{id}")]
    public async Task Delete([FromRoute] string id)
    {
        if (!await _permissionsService.Can(PermissionKey.Games_CreateEditDelete))
            throw new ActionForbidden();

        await ChallengeGateService.Delete(id);
        return;
    }

    /// <summary>
    /// Retrieve challenge gates
    /// </summary>
    /// <param name="g">game id</param>
    /// <returns></returns>
    [HttpGet("api/challengegates")]
    public async Task<ChallengeGate[]> List([FromQuery] string g)
    {
        if (!await _permissionsService.Can(PermissionKey.Games_CreateEditDelete))
            throw new ActionForbidden();

        return await ChallengeGateService.List(g);
    }
}
