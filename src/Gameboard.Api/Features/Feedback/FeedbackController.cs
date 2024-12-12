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
using Gameboard.Api.Features.Users;
using Gameboard.Api.Features.Feedback;
using MediatR;

namespace Gameboard.Api.Controllers;

[ApiController]
[Route("api/feedback")]
[Authorize]
public class FeedbackController
(
    IActingUserService actingUserService,
    ILogger<ChallengeController> logger,
    IDistributedCache cache,
    IMediator mediator,
    FeedbackValidator validator,
    FeedbackService feedbackService,
    IUserRolePermissionsService permissionsService
    ) : GameboardLegacyController(actingUserService, logger, cache, validator)
{
    private readonly IMediator _mediator = mediator;
    private readonly IUserRolePermissionsService _permissionsService = permissionsService;
    FeedbackService FeedbackService { get; } = feedbackService;

    /// <summary>
    /// Lists feedback based on search params
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    [HttpGet("list")]
    public async Task<FeedbackReportDetails[]> List([FromQuery] FeedbackSearchParams model)
    {
        await Authorize(_permissionsService.Can(PermissionKey.Reports_View));
        return await FeedbackService.List(model);
    }

    /// <summary>
    /// Gets feedback response
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    [HttpGet]
    public async Task<Feedback> Retrieve([FromQuery] FeedbackSearchParams model)
    {
        await Authorize(FeedbackService.UserIsEnrolled(model.GameId, Actor.Id));
        return await FeedbackService.Retrieve(model, Actor.Id);
    }

    /// <summary>
    /// Saves feedback response
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    [HttpPut("submit")]
    public async Task<Feedback> Submit([FromBody] FeedbackSubmissionLegacy model)
    {
        await Authorize(FeedbackService.UserIsEnrolled(model.GameId, Actor.Id));
        await Validate(model);

        var result = await FeedbackService.Submit(model, Actor.Id);

        return result;
    }

    /// <summary>
    /// Creates a new feedback template. Feedback templates can be used to gather feedback on a game,
    /// a game's challenges, or both.
    /// </summary>
    /// <param name="request"></param>
    /// <returns>The newly-created template</returns>
    [HttpPost("template")]
    public Task<FeedbackTemplateView> CreateTemplate([FromBody] CreateFeedbackTemplateRequest request)
        => _mediator.Send(new CreateFeedbackTemplateCommand(request));

    /// <summary>
    /// Deletes a feedback template.
    /// </summary>
    /// <param name="templateId"></param>
    /// <returns></returns>
    [HttpDelete("template/{templateId}")]
    public Task DeleteTemplate([FromRoute] string templateId)
        => _mediator.Send(new DeleteFeedbackTemplateCommand(templateId));

    [HttpGet("submission")]
    public Task<FeedbackSubmissionView> GetSubmission([FromQuery] GetFeedbackSubmissionRequest request)
        => _mediator.Send(new GetFeedbackSubmissionQuery(request));

    /// <summary>
    /// Retrieves a specific feedback template. Feedback templates can be used to gather feedback on a game,
    /// a game's challenges, or both.
    /// </summary>
    /// <param name="templateId"></param>
    /// <returns></returns>
    [HttpGet("template/{templateId}")]
    public Task<FeedbackTemplateView> GetTemplate([FromRoute] string templateId)
        => _mediator.Send(new GetFeedbackTemplateQuery(templateId));

    /// <summary>
    /// List all available feedback templates, including which games they're used with and how many responses
    /// they have.
    /// </summary>
    /// <returns>All feedback templates in the system</returns>
    [HttpGet("template")]
    public Task<ListFeedbackTemplatesResponse> ListTemplates()
        => _mediator.Send(new ListFeedbackTemplatesQuery());

    [HttpPut("template/{templateId}")]
    public Task<FeedbackTemplateView> UpdateTemplate([FromQuery] string templateId, [FromBody] UpdateFeedbackTemplateRequest request)
        => _mediator.Send(new UpdateFeedbackTemplateCommand(request));

    /// <summary>
    /// Create a new feedback response.
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpPost()]
    public Task<FeedbackSubmissionView> ResponseCreate([FromBody] UpsertFeedbackSubmissionRequest request)
        => _mediator.Send(new UpsertFeedbackSubmissionCommand(request));
}
