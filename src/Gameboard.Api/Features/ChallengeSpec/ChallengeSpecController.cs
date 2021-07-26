// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

﻿using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

using Gameboard.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Gameboard.Api.Validators;
using Microsoft.Extensions.Caching.Distributed;

namespace Gameboard.Api.Controllers
{
    [Authorize]
    public class ChallengeSpecController : _Controller
    {
        ChallengeSpecService ChallengeSpecService { get; }

        public ChallengeSpecController(
            ILogger<ChallengeSpecController> logger,
            IDistributedCache cache,
            ChallengeSpecValidator validator,
            ChallengeSpecService challengespecService
        ): base(logger, cache, validator)
        {
            ChallengeSpecService = challengespecService;
        }

        /// <summary>
        /// Create new challengespec
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost("api/challengespec")]
        [Authorize(AppConstants.DesignerPolicy)]
        public async Task<ChallengeSpec> Create([FromBody] NewChallengeSpec model)
        {
            await Validate(model);

            return await ChallengeSpecService.AddOrUpdate(model);
        }

        /// <summary>
        /// Retrieve challengespec
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("api/challengespec/{id}")]
        [Authorize(AppConstants.DesignerPolicy)]
        public async Task<ChallengeSpec> Retrieve([FromRoute]string id)
        {
            return await ChallengeSpecService.Retrieve(id);
        }

        /// <summary>
        /// Change challengespec
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPut("api/challengespec")]
        [Authorize(AppConstants.DesignerPolicy)]
        public async Task Update([FromBody] ChangedChallengeSpec model)
        {
            await ChallengeSpecService.Update(model);
            return;
        }

        /// <summary>
        /// Delete challengespec
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpDelete("/api/challengespec/{id}")]
        [Authorize(AppConstants.DesignerPolicy)]
        public async Task Delete([FromRoute]string id)
        {
            await ChallengeSpecService.Delete(id);
            return;
        }

        /// <summary>
        /// Find challengespecs
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpGet("/api/challengespecs")]
        // [Authorize(AppConstants.GameManagerPolicy)]
        [AllowAnonymous]
        public async Task<ExternalSpec[]> List([FromQuery] SearchFilter model)
        {
            return await ChallengeSpecService.List(model);
        }
    }
}
