// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

using Gameboard.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Gameboard.Api.Validators;
using Microsoft.Extensions.Caching.Distributed;

namespace Gameboard.Api.Controllers
{
    [Authorize(AppConstants.DesignerPolicy)]
    public class ChallengeGateController : _Controller
    {
        ChallengeGateService ChallengeGateService { get; }

        public ChallengeGateController(
            ILogger<ChallengeGateController> logger,
            IDistributedCache cache,
            ChallengeGateValidator validator,
            ChallengeGateService challengeGateService
        ): base(logger, cache, validator)
        {
            ChallengeGateService = challengeGateService;
        }

        /// <summary>
        /// Create new challenge gate
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost("api/challengegate")]
        [Authorize(AppConstants.DesignerPolicy)]
        public async Task<ChallengeGate> Create([FromBody] NewChallengeGate model)
        {
            await Validate(model);

            return await ChallengeGateService.AddOrUpdate(model);
        }

        /// <summary>
        /// Retrieve challenge gate
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("api/challengegate/{id}")]
        [Authorize(AppConstants.DesignerPolicy)]
        public async Task<ChallengeGate> Retrieve([FromRoute]string id)
        {
            return await ChallengeGateService.Retrieve(id);
        }

        /// <summary>
        /// Change challenge gate
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPut("api/challengegate")]
        [Authorize(AppConstants.DesignerPolicy)]
        public async Task Update([FromBody] ChangedChallengeGate model)
        {
            await ChallengeGateService.Update(model);
            return;
        }

        /// <summary>
        /// Delete challenge gate
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpDelete("/api/challengegate/{id}")]
        [Authorize(AppConstants.DesignerPolicy)]
        public async Task Delete([FromRoute]string id)
        {
            await ChallengeGateService.Delete(id);
            return;
        }

        /// <summary>
        /// Retrieve challenge gates
        /// </summary>
        /// <param name="g">game id</param>
        /// <returns></returns>
        [HttpGet("api/challengegates")]
        [Authorize(AppConstants.DesignerPolicy)]
        public async Task<ChallengeGate[]> List([FromQuery]string g)
        {
            return await ChallengeGateService.List(g);
        }
    }
}
