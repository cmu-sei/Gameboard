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
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Gameboard.Api.Auth;
using Gameboard.Api.Hubs;
using Gameboard.Api.Services;
using Gameboard.Api.Validators;
using Gameboard.Api.Features.Users;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace Gameboard.Api.Controllers
{
    [Authorize]
    public class UserController : _Controller
    {
        private readonly string _actingUserId;
        private readonly IMediator _mediator;

        UserService UserService { get; }
        CoreOptions Options { get; }
        IHubContext<AppHub, IAppHubEvent> Hub { get; }

        public UserController(
            ILogger<UserController> logger,
            IDistributedCache cache,
            UserValidator validator,
            UserService userService,
            CoreOptions options,
            IHttpContextAccessor httpContextAccessor,
            IMediator mediator,
            IHubContext<AppHub, IAppHubEvent> hub
        ) : base(logger, cache, validator)
        {
            UserService = userService;
            Options = options;
            Hub = hub;

            _actingUserId = httpContextAccessor.HttpContext.User.ToActor().Id;
            _mediator = mediator;
        }

        /// <summary>
        /// Register a new user
        /// </summary>
        /// <param name="model">NewUser</param>
        /// <remarks>Must be admin or self. Idempotent, so can be used as registration endpoint for ui initializer.</remarks>
        /// <returns>User</returns>
        [HttpPost("api/user")]
        [Authorize]
        public async Task<TryCreateUserResult> TryCreate([FromBody] NewUser model)
        {
            AuthorizeAny
            (
                () => Actor.IsRegistrar,
                () => model.Id == Actor.Id
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

        /// <summary>
        /// Get user-specific settings
        /// 
        /// NOTE: order is important here in the controller - need to avoid collisions with api/user/{id}.
        /// </summary>
        /// <returns></returns>
        [HttpGet("api/user/settings")]
        [Authorize]
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
        [Authorize]
        public async Task<User> Retrieve([FromRoute] string id)
        {
            AuthorizeAny(
                () => Actor.IsRegistrar,
                () => id == Actor.Id
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
        [Authorize]
        public async Task<User> Update([FromBody] ChangedUser model)
        {
            AuthorizeAny(
                () => Actor.IsRegistrar,
                () => model.Id == Actor.Id
            );

            await Validate(model);
            return await UserService.Update(model, Actor.IsRegistrar || Actor.IsAdmin, Actor.IsAdmin);
        }

        /// <summary>
        /// Delete user
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpDelete("/api/user/{id}")]
        [Authorize(AppConstants.RegistrarPolicy)]
        public async Task Delete([FromRoute] string id)
        {
            AuthorizeAny(() => Actor.IsRegistrar);
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
        /// <returns>User[]</returns>
        [HttpGet("/api/users")]
        [Authorize]
        public async Task<IEnumerable<UserOnly>> List([FromQuery] UserSearch model)
        {
            AuthorizeAny
            (
                () => Actor.IsRegistrar,
                () => Actor.IsObserver
            );

            return await UserService.List<UserOnly>(model);
        }

        /// <summary>
        /// Find users with SUPPORT role
        /// </summary>
        /// <param name="model"></param>
        /// <returns>User[]</returns>
        [HttpGet("/api/users/support")]
        [Authorize]
        public async Task<UserSimple[]> ListSupport([FromQuery] SearchFilter model)
        {
            AuthorizeAny(
                () => Actor.IsObserver,
                () => Actor.IsSupport
            );

            return await UserService.ListSupport(model);
        }

        /// <summary>
        /// Retrieve one-time-ticket to authenticate a signalr connection
        /// </summary>
        /// <remarks>Expires in 20s</remarks>
        /// <returns>{ "ticket": "value"}</returns>
        [HttpPost("/api/user/ticket")]
        [Authorize]
        public async Task<IActionResult> GetTicket()
        {
            string ticket = Guid.NewGuid().ToString("n");

            await Cache.SetStringAsync(
                $"{TicketAuthentication.TicketCachePrefix}{ticket}",
                $"{Actor.Id}#{Actor.Name}",

                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = new TimeSpan(0, 0, 20)
                }
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

        [HttpPost("/api/announce")]
        [Authorize]
        public async Task Announce([FromBody] Announcement model)
        {
            AuthorizeAny(
                () => Actor.IsDirector
            );

            var audience = string.IsNullOrEmpty(model.TeamId).Equals(false)
                ? Hub.Clients.Group(model.TeamId)
                : Hub.Clients.All;

            await audience.Announcement(new HubEvent<Announcement>
            {
                Model = model,
                Action = EventAction.Created,
                ActingUser = Actor.ToSimpleEntity()
            });
        }

        [HttpPut("/api/user/login")]
        [Authorize]
        public Task<UpdateUserLoginEventsResult> UpdateUserLoginEvents()
            => _mediator.Send(new UpdateUserLoginEventsCommand(_actingUserId));
    }
}
