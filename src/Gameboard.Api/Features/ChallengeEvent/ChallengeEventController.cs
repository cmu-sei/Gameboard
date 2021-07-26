// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

﻿using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

using Gameboard.Api.Services;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.AspNetCore.Authorization;

namespace Gameboard.Api.Controllers
{
    [Authorize(AppConstants.AdminPolicy)]
    public class ChallengeEventController : _Controller
    {
        ChallengeEventService ChallengeEventService { get; }

        public ChallengeEventController(
            ILogger<ChallengeEventController> logger,
            IDistributedCache cache,
            ChallengeEventService challengeeventService
        ): base(logger, cache)
        {
            ChallengeEventService = challengeeventService;
        }

        /// <summary>
        /// Create new challengeevent
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost("api/challengeevent")]
        public async Task<ChallengeEvent> Create([FromBody] NewChallengeEvent model)
        {
            return await ChallengeEventService.Create(model);
        }

        /// <summary>
        /// Retrieve challengeevent
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("api/challengeevent/{id}")]
        public async Task<ChallengeEvent> Retrieve([FromRoute]string id)
        {
            return await ChallengeEventService.Retrieve(id);
        }

        /// <summary>
        /// Change challengeevent
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPut("api/challengeevent")]
        public async Task Update([FromBody] ChangedChallengeEvent model)
        {
            await ChallengeEventService.Update(model);
            return;
        }

        /// <summary>
        /// Delete challengeevent
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpDelete("/api/challengeevent/{id}")]
        public async Task Delete([FromRoute]string id)
        {
            await ChallengeEventService.Delete(id);
            return;
        }

        /// <summary>
        /// Find challengeevents
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpGet("/api/challengeevents")]
        public async Task<ChallengeEvent[]> List([FromQuery] SearchFilter model)
        {
            return await ChallengeEventService.List(model);
        }
    }
}
