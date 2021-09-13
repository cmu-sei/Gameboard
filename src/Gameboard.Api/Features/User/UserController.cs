// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

using Gameboard.Api.Services;
using Microsoft.AspNetCore.Authorization;
using System;
using Microsoft.Extensions.Caching.Distributed;
using Gameboard.Api.Validators;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.SignalR;
using Gameboard.Api.Hubs;

namespace Gameboard.Api.Controllers
{
    [Authorize]
    public class UserController : _Controller
    {
        UserService UserService { get; }
        CoreOptions Options { get; }
        IHubContext<AppHub, IAppHubEvent> Hub { get; }

        public UserController(
            ILogger<UserController> logger,
            IDistributedCache cache,
            UserValidator validator,
            UserService userService,
            CoreOptions options,
            IHubContext<AppHub, IAppHubEvent> hub
        ): base(logger, cache, validator)
        {
            UserService = userService;
            Options = options;
            Hub = hub;
        }

        /// <summary>
        /// Register a new user
        /// </summary>
        /// <param name="model">NewUser</param>
        /// <remarks>Must be admin or self. Idempotent, so can be used as registration endpoint for ui initializer.</remarks>
        /// <returns>User</returns>
        [HttpPost("api/user")]
        [Authorize]
        public async Task<User> Create([FromBody] NewUser model)
        {
            AuthorizeAny(
                () => Actor.IsRegistrar,
                () => model.Id == Actor.Id
            );

            await Validate(model);

            var user = await UserService.GetOrAdd(model);

            await HttpContext.SignInAsync(
                AppConstants.MksCookie,
                new ClaimsPrincipal(
                    new ClaimsIdentity(User.Claims, AppConstants.MksCookie)
                )
            );

            return user;
        }

        /// <summary>
        /// Retrieve user
        /// </summary>
        /// <param name="id"></param>
        /// <returns>User</returns>
        [HttpGet("api/user/{id}")]
        [Authorize]
        public async Task<User> Retrieve([FromRoute]string id)
        {
            AuthorizeAny(
                () => Actor.IsRegistrar,
                () => id == Actor.Id
            );

            await Validate(new Entity{ Id = id });

            return await UserService.Retrieve(id);
        }

        /// <summary>
        /// Change user
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPut("api/user")]
        [Authorize]
        public async Task Update([FromBody] ChangedUser model)
        {
            AuthorizeAny(
                () => Actor.IsRegistrar,
                () => model.Id == Actor.Id
            );

            await Validate(model);

            await UserService.Update(model, Actor.IsRegistrar);
        }

        /// <summary>
        /// Delete user
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpDelete("/api/user/{id}")]
        [Authorize(AppConstants.RegistrarPolicy)]
        public async Task Delete([FromRoute]string id)
        {
            AuthorizeAny(
                () => Actor.IsRegistrar
            );

            await Validate(new Entity{ Id = id });

            await UserService.Delete(id);
        }

        /// <summary>
        /// Find users
        /// </summary>
        /// <param name="model"></param>
        /// <returns>User[]</returns>
        [HttpGet("/api/users")]
        [Authorize(AppConstants.RegistrarPolicy)]
        public async Task<User[]> List([FromQuery] UserSearch model)
        {
            AuthorizeAny(
                () => Actor.IsRegistrar
            );

            return await UserService.List(model);
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

                new DistributedCacheEntryOptions {
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
                : Hub.Clients.All
            ;

            await audience.Announcement(new HubEvent<Announcement>(model, EventAction.Created));
        }

        /// <summary>
        /// check version
        /// </summary>
        /// <returns></returns>
        [HttpGet("/api/version")]
        [AllowAnonymous]
        public IActionResult Version()
        {
            return Ok(new {
                Commit = Environment.GetEnvironmentVariable("COMMIT") ?? "no version info"
            });
        }

    }
}
