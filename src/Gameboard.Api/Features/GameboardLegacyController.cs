// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Threading.Tasks;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace Gameboard.Api.Controllers;

public abstract class GameboardLegacyController(
        IActingUserService actingUserService,
        ILogger logger,
        IDistributedCache cache,
        params IModelValidator[] validators
        ) : ControllerBase
{
    private readonly IActingUserService _actingUserService = actingUserService;

    protected internal User Actor
    {
        get => _actingUserService.Get();
    }

    protected string AuthenticatedGraderForChallengeId
    {
        get => HttpContext.Items[AppConstants.RequestContextGameboardGraderForChallengeId]?.ToString();
    }

    protected ILogger Logger { get; private set; } = logger;
    protected IDistributedCache Cache { get; private set; } = cache;
    private readonly IModelValidator[] _validators = validators;

    /// <summary>
    /// Validate a model against all validators registered
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    protected async Task Validate(object model)
    {
        foreach (var v in _validators)
            await v.Validate(model);
    }

    protected async Task Authorize(Task<bool> requirement)
    {
        if (!await requirement)
            throw new ActionForbidden();
    }

    /// <summary>
    /// Authorize if all requirements are met
    /// </summary>
    /// <param name="requirements"></param>
    protected void AuthorizeAll(params Func<bool>[] requirements)
    {
        bool valid = true;

        foreach (var requirement in requirements)
            valid &= requirement.Invoke();

        if (valid.Equals(false))
            throw new ActionForbidden();
    }

    /// <summary>
    /// Authorized if any requirement is met
    /// </summary>
    /// <param name="requirements"></param>
    protected void AuthorizeAny(params Func<bool>[] requirements)
    {
        if (Actor?.Role == UserRoleKey.Admin)
            return;

        bool valid = false;

        foreach (var requirement in requirements)
        {
            valid |= requirement.Invoke();
            if (valid) break;
        }

        if (valid.Equals(false))
            throw new ActionForbidden();
    }

    protected async Task AuthorizeAny(params Func<Task<bool>>[] requirements)
    {
        if (Actor?.Role == UserRoleKey.Admin)
            return;

        foreach (var requirement in requirements)
        {
            if (await requirement.Invoke())
                return;
        }

        throw new ActionForbidden();
    }
}
