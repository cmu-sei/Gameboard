// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Threading.Tasks;
using Gameboard.Api.Services;
using Gameboard.Api.Validators;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace Gameboard.Api.Controllers
{
    [Authorize(AppConstants.DesignerPolicy)]
    public class ChallengeSpecController : _Controller
    {
        ChallengeSpecService ChallengeSpecService { get; }

        public ChallengeSpecController(
            ILogger<ChallengeSpecController> logger,
            IDistributedCache cache,
            ChallengeSpecValidator validator,
            ChallengeSpecService challengespecService
        ) : base(logger, cache, validator)
        {
            ChallengeSpecService = challengespecService;
        }

        /// <summary>
        /// Create a new challengespec.
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
        public async Task Delete([FromRoute] string id)
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
        [Authorize(AppConstants.DesignerPolicy)]
        public async Task<ExternalSpec[]> List([FromQuery] SearchFilter model)
        {
            return await ChallengeSpecService.List(model);
        }

        /// <summary>
        /// Sync challengespec name/description with external source
        /// </summary>
        /// <param name="id">game id</param>
        /// <returns></returns>
        [HttpPost("/api/challengespecs/sync/{id}")]
        [Authorize(AppConstants.DesignerPolicy)]
        public async Task Sync([FromRoute] string id)
        {
            await ChallengeSpecService.Sync(id);
        }

        /// <summary>
        /// Find challengespecs
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpGet("/api/practice")]
        [AllowAnonymous]
        public async Task<ChallengeSpecSummary[]> Browse([FromQuery] SearchFilter model)
        {
            return await ChallengeSpecService.Browse(model);
        }
    }
}
