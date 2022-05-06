// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

using Gameboard.Api.Services;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.AspNetCore.Authorization;
using Gameboard.Api.Validators;

namespace Gameboard.Api.Controllers
{
    [Authorize]
    public class FeedbackController : _Controller
    {
        ChallengeService ChallengeService { get; }
        FeedbackService FeedbackService { get; }
        PlayerService PlayerService { get; }

        public FeedbackController(
            ILogger<ChallengeController> logger,
            IDistributedCache cache,
            FeedbackValidator validator,
            ChallengeService challengeService,
            FeedbackService feedbackService,
            PlayerService playerService
        ): base(logger, cache, validator)
        {
            ChallengeService = challengeService;
            FeedbackService = feedbackService;
            PlayerService = playerService;
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
            AuthorizeAny(
                () => FeedbackService.UserIsEnrolled(model.GameId, Actor.Id).Result
            );
            return await FeedbackService.Retrieve(model, Actor.Id);
        }


        /// <summary>
        /// Saves feedback response
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPut("/api/feedback/submit")]
        [Authorize]
        public async Task<Feedback> Submit([FromBody]FeedbackSubmission model)
        {
            AuthorizeAny(
                () => FeedbackService.UserIsEnrolled(model.GameId, Actor.Id).Result
            );

            await Validate(model);

            var result = await FeedbackService.Submit(model, Actor.Id);

            return result;
        }

        /// <summary>
        /// Lists feedback based on search params
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpGet("/api/feedback/list")]
        [Authorize]
        public async Task<FeedbackReportDetails[]> List([FromQuery] FeedbackSearchParams model)
        {
            AuthorizeAny(
                () => Actor.IsObserver
            );
            FeedbackReportDetails[] result = await FeedbackService.List(model);
            return result;
        }

    }
}
