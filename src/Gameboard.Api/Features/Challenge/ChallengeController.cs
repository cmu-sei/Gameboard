// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

﻿using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

using Gameboard.Api.Services;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.AspNetCore.Authorization;
using TopoMojo.Api.Client;
using Gameboard.Api.Validators;

namespace Gameboard.Api.Controllers
{
    [Authorize]
    public class ChallengeController : _Controller
    {
        ChallengeService ChallengeService { get; }
        PlayerService PlayerService { get; }

        public ChallengeController(
            ILogger<ChallengeController> logger,
            IDistributedCache cache,
            ChallengeValidator validator,
            ChallengeService challengeService,
            PlayerService playerService
        ): base(logger, cache, validator)
        {
            ChallengeService = challengeService;
            PlayerService = playerService;
        }

        /// <summary>
        /// Create new challenge instance
        /// </summary>
        /// <remarks>Idempotent method to retrieve or create challenge state</remarks>
        /// <param name="model">NewChallenge</param>
        /// <returns>Challenge</returns>
        [HttpPost("api/challenge")]
        [Authorize]
        public async Task<Challenge> Create([FromBody] NewChallenge model)
        {
            AuthorizeAny(
                () => Actor.IsDirector,
                () => IsSelf(model.PlayerId).Result
            );

            await Validate(model);

            if (Actor.IsTester.Equals(false))
                model.Variant = 0;

            return await ChallengeService.GetOrAdd(model);
        }

        /// <summary>
        /// Retrieve challenge
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("api/challenge/{id}")]
        [Authorize]
        public async Task<Challenge> Retrieve([FromRoute]string id)
        {
            AuthorizeAny(
                () => Actor.IsDirector,
                () => ChallengeService.UserIsTeamPlayer(id, Actor.Id).Result
            );

            await Validate(new Entity{ Id = id });

            return await ChallengeService.Retrieve(id);
        }

        /// <summary>
        /// Retrieve challenge preview
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost("api/challenge/preview")]
        [Authorize]
        public async Task<Challenge> Preview([FromBody]NewChallenge model)
        {
            AuthorizeAny(
                () => IsSelf(model.PlayerId).Result
            );

            await Validate(model);

            return await ChallengeService.Preview(model);
        }

        /// <summary>
        /// Change challenge
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPut("api/challenge")]
        [Authorize]
        public async Task Update([FromBody] ChangedChallenge model)
        {
            await ChallengeService.Update(model);
            return;
        }

        /// <summary>
        /// Delete challenge
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpDelete("/api/challenge/{id}")]
        [Authorize]
        public async Task Delete([FromRoute]string id)
        {
            AuthorizeAny(
                () => Actor.IsDirector,
                () => Actor.IsTester && ChallengeService.UserIsTeamPlayer(id, Actor.Id).Result
            );

            await Validate(new Entity{ Id = id });

            await ChallengeService.Delete(id);
            return;
        }

        /// <summary>
        /// Start a  challenge gamespace
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPut("/api/challenge/start")]
        [Authorize]
        public async Task<Challenge> StartGamespace([FromBody]ChangedChallenge model)
        {
            AuthorizeAny(
                () => Actor.IsDirector,
                () => ChallengeService.UserIsTeamPlayer(model.Id, Actor.Id).Result
            );

            await Validate(model);

            return await ChallengeService.StartGamespace(model.Id);
        }

        /// <summary>
        /// Stop a challenge gamespace
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPut("/api/challenge/stop")]
        [Authorize]
        public async Task<Challenge> StopGamespace([FromBody]ChangedChallenge model)
        {
            AuthorizeAny(
                () => Actor.IsDirector,
                () => ChallengeService.UserIsTeamPlayer(model.Id, Actor.Id).Result
            );

            await Validate(new Entity{ Id = model.Id });

            return await ChallengeService.StopGamespace(model.Id);
        }

        /// <summary>
        /// Grade a challenge
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPut("/api/challenge/grade")]
        [Authorize]
        public async Task<Challenge> Grade([FromBody]SectionSubmission model)
        {
            AuthorizeAny(
                () => Actor.IsDirector,
                () => ChallengeService.UserIsTeamPlayer(model.Id, Actor.Id).Result
            );

            await Validate(new Entity{ Id = model.Id });

            return await ChallengeService.Grade(model);
        }

        /// <summary>
        /// Console action (ticket, reset)
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost("/api/challenge/console")]
        [Authorize(AppConstants.ConsolePolicy)]
        public async Task<ConsoleSummary> GetConsole([FromBody]ConsoleRequest model)
        {
            var isTeamMember = await ChallengeService.UserIsTeamPlayer(model.SessionId, Actor.Id);

            AuthorizeAny(
              () => Actor.IsDirector,
              () => Actor.IsObserver,
              () => isTeamMember
            );

            return await ChallengeService.GetConsole(model, isTeamMember.Equals(false));
        }

        /// <summary>
        /// Find challenges
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpGet("/api/challenges")]
        [Authorize]
        public async Task<Challenge[]> List([FromQuery] SearchFilter model)
        {
            AuthorizeAny(
                () => Actor.IsDirector
            );

            return await ChallengeService.List(model);
        }

        private async Task<bool> IsSelf(string playerId)
        {
          return await PlayerService.MapId(playerId) == Actor.Id;
        }
    }
}
