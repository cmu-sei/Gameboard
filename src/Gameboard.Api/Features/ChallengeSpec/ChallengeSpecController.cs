// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Threading.Tasks;
using Gameboard.Api.Features.ChallengeSpecs;
using Gameboard.Api.Services;
using Gameboard.Api.Validators;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace Gameboard.Api.Controllers
{
    [Authorize(AppConstants.DesignerPolicy)]
    public class ChallengeSpecController : _Controller
    {
        IMediator _mediator;
        ChallengeSpecService ChallengeSpecService { get; }

        public ChallengeSpecController(
            ILogger<ChallengeSpecController> logger,
            IDistributedCache cache,
            IMediator mediator,
            ChallengeSpecValidator validator,
            ChallengeSpecService challengespecService
        ) : base(logger, cache, validator)
        {
            ChallengeSpecService = challengespecService;
            _mediator = mediator;
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
        public Task<ChallengeSpec> Retrieve([FromRoute] string id)
            => ChallengeSpecService.Retrieve(id);

        /// <summary>
        /// Change challengespec
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPut("api/challengespec")]
        [Authorize(AppConstants.DesignerPolicy)]
        public Task Update([FromBody] ChangedChallengeSpec model)
            => ChallengeSpecService.Update(model);

        /// <summary>
        /// Delete challengespec
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpDelete("/api/challengespec/{id}")]
        [Authorize(AppConstants.DesignerPolicy)]
        public Task Delete([FromRoute] string id)
            => ChallengeSpecService.Delete(id);

        /// <summary>
        /// Find challengespecs
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpGet("/api/challengespecs")]
        [Authorize(AppConstants.DesignerPolicy)]
        public Task<ExternalSpec[]> List([FromQuery] SearchFilter model)
            => ChallengeSpecService.List(model);

        /// <summary>
        /// Load solve performance for the challenge spec
        /// </summary>
        [HttpGet("/api/challengespecs/{challengeSpecId}/question-performance")]
        [Authorize(AppConstants.AdminPolicy)]
        public Task<GetChallengeSpecQuestionPerformanceResult> GetQuestionPerformance([FromRoute] string challengeSpecId)
            => _mediator.Send(new GetChallengeSpecQuestionPerformanceQuery(challengeSpecId));

        /// <summary>
        /// Sync challengespec name/description with external source
        /// </summary>
        /// <param name="id">game id</param>
        /// <returns></returns>
        [HttpPost("/api/challengespecs/sync/{id}")]
        [Authorize(AppConstants.DesignerPolicy)]
        public Task Sync([FromRoute] string id)
            => ChallengeSpecService.Sync(id);
    }
}
