// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Gameboard.Api.Auth;
using Gameboard.Api.Services;
using Gameboard.Api.Validators;
using Gameboard.Api.Features.Users;
using MediatR;
using Microsoft.AspNetCore.Http;
using Gameboard.Api.Common.Services;
using System.Threading;

namespace Gameboard.Api.Controllers;

[Authorize]
public class UserController(
    IActingUserService actingUserService,
    IGuidService guids,
    ILogger<UserController> logger,
    IDistributedCache cache,
    UserValidator validator,
    UserService userService,
    CoreOptions options,
    IMediator mediator,
    IUserRolePermissionsService permissionsService
    ) : _Controller(actingUserService, logger, cache, validator)
{
    private readonly IActingUserService _actingUserService = actingUserService;
    private readonly IGuidService _guids = guids;
    private readonly IMediator _mediator = mediator;
    private readonly IUserRolePermissionsService _permissionsService = permissionsService;

    UserService UserService { get; } = userService;
    CoreOptions Options { get; } = options;

    /// <summary>
    /// Register a new user
    /// </summary>
    /// <param name="model">NewUser</param>
    /// <remarks>Must be admin or self. Idempotent, so can be used as registration endpoint for ui initializer.</remarks>
    /// <returns>User</returns>
    [HttpPost("api/user")]
    public async Task<TryCreateUserResult> TryCreate([FromBody] NewUser model)
    {
        await AuthorizeAny
        (
            Task.FromResult(model.Id == Actor?.Id),
            _permissionsService.Can(PermissionKey.Users_CreateEditDelete)
        );

        var result = await UserService.TryCreate(model);

        await HttpContext.SignInAsync(
            AppConstants.MksCookie,
            new ClaimsPrincipal(
                new ClaimsIdentity(User.Claims, AppConstants.MksCookie)
            )
        );

        return result;
    }

    [HttpPost("api/users")]
    public Task<TryCreateUsersResponse> TryCreateMany([FromBody] TryCreateUsersRequest request)
        => _mediator.Send(new TryCreateUsersCommand(request));

    /// <summary>
    /// Get user-specific settings
    /// 
    /// NOTE: order is important here in the controller - need to avoid collisions with api/user/{id}.
    /// </summary>
    /// <returns></returns>
    [HttpGet("api/user/settings")]
    public Task<UserSettings> GetSettings()
        => _mediator.Send(new GetUserSettingsQuery());

    [HttpPut("api/user/settings")]
    public Task<UserSettings> UpdateSettings([FromBody] UpdateUserSettingsRequest request)
        => _mediator.Send(new UpdateUserSettingsCommand(request));

    /// <summary>
    /// Retrieve user
    /// </summary>
    /// <param name="id"></param>
    /// <returns>User</returns>
    [HttpGet("api/user/{id}")]
    public async Task<User> Retrieve([FromRoute] string id)
    {
        await AuthorizeAny
        (
            _permissionsService.Can(PermissionKey.Admin_View),
            Task.FromResult(id == Actor.Id)
        );

        await Validate(new Entity { Id = id });
        return await UserService.Retrieve(id);
    }

    /// <summary>
    /// Change user
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    [HttpPut("api/user")]
    public async Task<User> Update([FromBody] ChangedUser model)
    {
        var canAdminUsers = await _permissionsService.Can(PermissionKey.Users_CreateEditDelete);
        AuthorizeAny
        (
            () => model.Id == Actor.Id,
            () => canAdminUsers
        );

        await Validate(model);
        return await UserService.Update(model, canAdminUsers);
    }

    /// <summary>
    /// Delete user
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [HttpDelete("/api/user/{id}")]
    public async Task Delete([FromRoute] string id)
    {
        await AuthorizeAny(_permissionsService.Can(PermissionKey.Users_CreateEditDelete));
        await Validate(new Entity { Id = id });
        await UserService.Delete(id);
    }

    /// <summary>
    /// Get the user's active challenges.
    /// </summary>
    /// <param name="userId"></param>
    /// <returns></returns>
    [HttpGet("/api/user/{userId}/challenges/active")]
    public Task<UserActiveChallenges> GetUserActiveChallenges([FromRoute] string userId)
            => _mediator.Send(new GetUserActiveChallengesQuery(userId));

    /// <summary>
    /// Find users
    /// </summary>
    /// <param name="model"></param>
    /// <param name="cancellationToken"></param>
    /// <returns>UserOnly[]</returns>
    [HttpGet("/api/users")]
    public async Task<IEnumerable<UserOnly>> List([FromQuery] UserSearch model, CancellationToken cancellationToken)
    {
        await AuthorizeAny(_permissionsService.Can(PermissionKey.Admin_View));
        return await UserService.List<UserOnly>(model);
    }

    /// <summary>
    /// Find users with SUPPORT role
    /// </summary>
    /// <param name="model"></param>
    /// <returns>User[]</returns>
    [HttpGet("/api/users/support")]
    public async Task<UserSimple[]> ListSupport([FromQuery] SearchFilter model)
    {
        await AuthorizeAny(_permissionsService.Can(PermissionKey.Support_ManageTickets));
        return await UserService.ListSupport(model);
    }

    /// <summary>
    /// Retrieve one-time-ticket to authenticate a signalr connection
    /// </summary>
    /// <remarks>Expires in 20s</remarks>
    /// <returns>{ "ticket": "value"}</returns>
    [HttpPost("/api/user/ticket")]
    public async Task<IActionResult> GetTicket()
    {
        string ticket = _guids.GetGuid();

        await Cache.SetStringAsync(
            $"{TicketAuthentication.TicketCachePrefix}{ticket}",
            $"{Actor.Id}#{Actor.Name}",
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = new TimeSpan(0, 0, 20) }
        );

        return Ok(new { Ticket = ticket });
    }

    /// <summary>
    /// Auth check fails if cookie expired
    /// </summary>
    /// <returns></returns>
    [HttpGet("/api/user/ping")]
    [Authorize(AppConstants.ConsolePolicy)]
    public IActionResult Heartbeat()
    {
        return Ok();
    }

    [HttpPost("/api/user/logout")]
    [Authorize(AppConstants.ConsolePolicy)]
    public async Task<IActionResult> Logout()
    {
        if (User.Identity.AuthenticationType == AppConstants.MksCookie)
            await HttpContext.SignOutAsync(AppConstants.MksCookie);

        return Ok();
    }


    [HttpGet("/api/docs")]
    [AllowAnonymous]
    public string[] GetDocList()
    {
        var result = Directory.GetFiles(Options.DocFolder, "*", SearchOption.AllDirectories)
            .Select(x => x.Replace(Options.DocFolder, ""))
            .ToArray()
        ;
        return result;
    }

    [HttpPut("/api/user/login")]
    public Task<UpdateUserLoginEventsResult> UpdateUserLoginEvents()
        => _mediator.Send(new UpdateUserLoginEventsCommand(_actingUserService.Get().Id));

    [HttpGet("/api/users/roles/permissions")]
    public Task<UserRolePermissionsOverviewResponse> GetUserRolePermissionsOverview()
        => _mediator.Send(new UserRolePermissionsOverviewQuery());
}
