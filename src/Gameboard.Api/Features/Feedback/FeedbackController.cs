// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Gameboard.Api.Services;
using Gameboard.Api.Validators;
using Gameboard.Api.Common.Services;

namespace Gameboard.Api.Controllers
{
    [Authorize]
    public class FeedbackController : _Controller
    {
        FeedbackService FeedbackService { get; }

        public FeedbackController(
            IActingUserService actingUserService,
            ILogger<ChallengeController> logger,
            IDistributedCache cache,
            FeedbackValidator validator,
            ChallengeService challengeService,
            FeedbackService feedbackService,
            PlayerService playerService
        ) : base(actingUserService, logger, cache, validator)
        {
            FeedbackService = feedbackService;
        }

        /// <summary>
        /// Gets feedback response
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpGet("api/feedback")]
        [Authorize]
        public async Task<Feedback> Retrieve([FromQuery] FeedbackSearchParams model)
        {
            AuthorizeAny(() => FeedbackService.UserIsEnrolled(model.GameId, Actor.Id).Result);

            return await FeedbackService.Retrieve(model, Actor.Id);
        }

        /// <summary>
        /// Saves feedback response
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPut("/api/feedback/submit")]
        [Authorize]
        public async Task<Feedback> Submit([FromBody] FeedbackSubmission model)
        {
            AuthorizeAny(
                () => FeedbackService.UserIsEnrolled(model.GameId, Actor.Id).Result
            );

            await Validate(model);

            var result = await FeedbackService.Submit(model, Actor.Id);

            return result;
        }
    }
}
