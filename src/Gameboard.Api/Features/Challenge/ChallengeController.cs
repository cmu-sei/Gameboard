// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Challenges;
using Gameboard.Api.Features.Consoles;
using Gameboard.Api.Features.GameEngine;
using Gameboard.Api.Features.Users;
using Gameboard.Api.Hubs;
using Gameboard.Api.Services;
using Gameboard.Api.Validators;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace Gameboard.Api.Controllers;

[Authorize]
public class ChallengeController
(
    IActingUserService actingUserService,
    ILogger<ChallengeController> logger,
    IDistributedCache cache,
    ChallengeValidator validator,
    ChallengeService challengeService,
    IGameEngineService gameEngineService,
    IMediator mediator,
    IUserRolePermissionsService permissionsService,
    PlayerService playerService,
    IHubContext<AppHub, IAppHubEvent> hub,
    ConsoleActorMap actorMap
    ) : GameboardLegacyController(actingUserService, logger, cache, validator)
{
    private readonly IMediator _mediator = mediator;
    private readonly IUserRolePermissionsService _permissionsService = permissionsService;

    ChallengeService ChallengeService { get; } = challengeService;
    PlayerService PlayerService { get; } = playerService;
    IHubContext<AppHub, IAppHubEvent> Hub { get; } = hub;
    ConsoleActorMap ActorMap { get; } = actorMap;

    /// <summary>
    /// Purge a challenge. This deletes the challenge instance, and all progress on it, and can't be undone.
    /// A record of the challenge is stored in ArchivedChallenges.
    /// </summary>
    /// <param name="challengeId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpDelete("api/challenge/{challengeId}")]
    public Task DeleteChallenge(string challengeId, CancellationToken cancellationToken)
        => _mediator.Send(new PurgeChallengeCommand(challengeId), cancellationToken);

    /// <summary>
    /// Create and start an instance of a challenge.
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpPost("api/challenge")]
    public Task<StartChallengeResponse> Create([FromBody] StartChallengeCommand request, CancellationToken cancellationToken)
        => _mediator.Send(request, cancellationToken);

    /// <summary>
    /// Create new challenge instance.
    /// 
    /// (This endpoint is deprecated and will be removed in a future release. Instead, use api/challenge and pass teamId + specId)
    /// </summary>
    /// <remarks>Idempotent method to retrieve or create challenge state</remarks>
    /// <param name="model">NewChallenge</param>
    /// <returns>Challenge</returns>
    [HttpPost("api/challenge/launch")]
    public async Task<Challenge> Create([FromBody] NewChallenge model)
    {
        await AuthorizeAny
        (
            () => _permissionsService.Can(PermissionKey.Teams_DeployGameResources),
            () => IsSelf(model.PlayerId)
        );
        await Validate(model);

        if (!await _permissionsService.Can(PermissionKey.Play_ChooseChallengeVariant))
            model.Variant = 0;

        var result = await ChallengeService.GetOrCreate(model, Actor.Id);

        await Hub.Clients.Group(result.TeamId).ChallengeEvent(new HubEvent<Challenge>
        {
            Model = result,
            Action = EventAction.Updated,
            ActingUser = Actor.ToSimpleEntity()
        });

        return result;
    }

    /// <summary>
    /// Retrieve challenge
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [HttpGet("api/challenge/{id}")]
    public async Task<Challenge> Retrieve([FromRoute] string id)
    {
        await AuthorizeAny
        (
            () => _permissionsService.Can(PermissionKey.Teams_Observe),
            () => ChallengeService.UserIsPlayingChallenge(id, Actor.Id)
        );

        await Validate(new Entity { Id = id });
        return await ChallengeService.Get(id);
    }

    /// <summary>
    /// Retrieve challenge preview
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    [HttpPost("api/challenge/preview")]
    public async Task<Challenge> Preview([FromBody] NewChallenge model)
    {
        await Authorize(IsSelf(model.PlayerId));
        await Validate(model);

        return await ChallengeService.Preview(model);
    }

    /// <summary>
    /// Start a  challenge gamespace
    /// </summary>
    /// <param name="model"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpPut("/api/challenge/start")]
    public async Task<Challenge> StartGamespace([FromBody] ChangedChallenge model, CancellationToken cancellationToken)
    {
        await AuthorizeAny
        (
            () => _permissionsService.Can(PermissionKey.Teams_DeployGameResources),
            () => ChallengeService.UserIsPlayingChallenge(model.Id, Actor.Id)
        );

        await Validate(model);

        var result = await ChallengeService.StartGamespace(model.Id, Actor.Id, cancellationToken);

        await Hub.Clients.Group(result.TeamId).ChallengeEvent
        (
            new HubEvent<Challenge>
            {
                Model = result,
                Action = EventAction.Updated,
                ActingUser = Actor.ToSimpleEntity()
            }
        );

        return result;
    }

    /// <summary>
    /// Stop a challenge gamespace
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    [HttpPut("/api/challenge/stop")]
    public async Task<Challenge> StopGamespace([FromBody] ChangedChallenge model)
    {
        await AuthorizeAny
        (
            () => _permissionsService.Can(PermissionKey.Teams_DeployGameResources),
            () => ChallengeService.UserIsPlayingChallenge(model.Id, Actor.Id)
        );

        await Validate(new Entity { Id = model.Id });

        var result = await ChallengeService.StopGamespace(model.Id, Actor.Id);

        await Hub.Clients.Group(result.TeamId).ChallengeEvent
        (
            new HubEvent<Challenge>
            {
                Model = result,
                Action = EventAction.Updated,
                ActingUser = Actor.ToSimpleEntity()
            }
        );

        return result;
    }

    [HttpPut("/api/challenge/{challengeId}/sync")]
    public Task<GameEngineGameState> Sync([FromRoute] string challengeId, CancellationToken cancellationToken)
        => _mediator.Send(new SyncChallengeCommand(challengeId), cancellationToken);

    /// <summary>
    /// Grade a challenge
    /// </summary>
    /// <param name="model"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpPost("/api/challenge/grade")]
    [HttpPut("/api/challenge/grade")]
    [Authorize(AppConstants.GraderPolicy)]
    public async Task<Challenge> Grade([FromBody] GameEngineSectionSubmission model, CancellationToken cancellationToken)
    {
        await AuthorizeAny
        (
            // this is set by _Controller if the caller authenticated with a grader key
            () => Task.FromResult(AuthenticatedGraderForChallengeId == model.Id),
            // these are set if the caller authenticated with standard JWT
            () => _permissionsService.Can(PermissionKey.Teams_DeployGameResources),
            () => ChallengeService.UserIsPlayingChallenge(model.Id, Actor.Id)
        );

        await Validate(new Entity { Id = model.Id });

        var result = await ChallengeService.Grade(model, Actor, cancellationToken);

        await Hub.Clients.Group(result.TeamId).ChallengeEvent(
            new HubEvent<Challenge>
            {
                Model = result,
                Action = EventAction.Updated,
                ActingUser = Actor.ToSimpleEntity()
            }
        );

        return result;
    }

    /// <summary>
    /// Regrade a challenge
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    [HttpPut("/api/challenge/regrade")]
    public async Task<Challenge> Regrade([FromBody] Entity model)
    {
        await Authorize(_permissionsService.Can(PermissionKey.Scores_RegradeAndRerank));
        await Validate(model);
        var result = await ChallengeService.Regrade(model.Id);

        await Hub.Clients.Group(result.TeamId).ChallengeEvent
        (
            new HubEvent<Challenge>
            {
                Model = result,
                Action = EventAction.Updated,
                ActingUser = Actor.ToSimpleEntity()
            });

        return result;
    }

    /// <summary>
    /// ReGrade a challenge
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [HttpGet("/api/challenge/{id}/audit")]
    public async Task<IEnumerable<GameEngineSectionSubmission>> Audit([FromRoute] string id)
    {
        await Authorize(_permissionsService.Can(PermissionKey.Teams_Observe));
        await Validate(new Entity { Id = id });
        return await ChallengeService.Audit(id);
    }

    /// <summary>
    /// Console action (ticket, reset)
    /// </summary>
    /// <param name="model"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpPost("/api/challenge/console")]
    [Authorize(AppConstants.ConsolePolicy)]
    public async Task<ConsoleState> SendConsoleCommand([FromBody] ConsoleRequest model, CancellationToken cancellationToken)
    {
        await Validate(new Entity { Id = model.SessionId });
        var isTeamMember = await ChallengeService.UserIsPlayingChallenge(model.SessionId, Actor.Id);
        Logger.LogInformation($"Console access attempt ({model.Id} / {Actor.Id}): User {Actor.Id}, roles {Actor.Role}, on team = {isTeamMember} .");

        if (!isTeamMember)
            await Authorize(_permissionsService.Can(PermissionKey.Teams_Observe));

        Logger.LogInformation($"""Console access attempt ({model.Id} / {Actor.Id}): Allowed.""");
        var result = await gameEngineService.GetConsole(GameEngineType.TopoMojo, new ConsoleId { ChallengeId = model.SessionId, Name = model.Name }, cancellationToken);

        if (isTeamMember)
        {
            ActorMap.Update(await ChallengeService.SetConsoleActor(new ConsoleId
            {
                ChallengeId = model.SessionId,
                Name = model.Name
            }, Actor.Id, Actor.ApprovedName));
        }

        return result;
    }

    [HttpGet("/api/challenge/consoles")]
    public async Task<List<ObserveChallenge>> FindConsoles([FromQuery] string gid)
    {
        await Authorize(_permissionsService.Can(PermissionKey.Teams_Observe));
        return await ChallengeService.GetChallengeConsoles(gid);
    }

    [HttpGet("/api/challenge/consoleactors")]
    public async Task<ConsoleActor[]> GetConsoleActors([FromQuery] string gid)
    {
        await Authorize(_permissionsService.Can(PermissionKey.Teams_Observe));
        return ChallengeService.GetConsoleActors(gid);
    }

    [HttpGet("/api/challenge/consoleactor")]
    [Authorize(AppConstants.ConsolePolicy)]
    public async Task<ConsoleActor> GetConsoleActor([FromQuery] string uid)
    {
        await Authorize(_permissionsService.Can(PermissionKey.Teams_Observe));
        return ChallengeService.GetConsoleActor(uid);
    }

    [HttpGet("api/challenge/{challengeId}/progress")]
    public Task<GetChallengeProgressResponse> GetProgress([FromRoute] string challengeId)
        => _mediator.Send(new GetChallengeProgressQuery(challengeId));

    [HttpGet("api/challenge/{challengeId}/solution-guide")]
    public Task<ChallengeSolutionGuide> GetSolutionGuide([FromRoute] string challengeId)
        => _mediator.Send(new GetChallengeSolutionGuideQuery(challengeId));

    [HttpGet("api/challenge/{challengeId}/submissions")]
    public Task<GetChallengeSubmissionsResponse> GetSubmissions([FromRoute] string challengeId)
        => _mediator.Send(new GetChallengeSubmissionsQuery(challengeId));

    [HttpPut("api/challenge/{challengeId}/submissions/pending")]
    public Task UpdatePendingSubmission([FromRoute] string challengeId, [FromBody] ChallengeSubmissionAnswers submission)
        => _mediator.Send(new SaveChallengePendingSubmissionCommand(challengeId, submission));

    /// <summary>
    /// Find challenges
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    [HttpGet("/api/challenges")]
    public async Task<ChallengeSummary[]> List([FromQuery] SearchFilter model)
    {
        await Authorize(_permissionsService.Can(PermissionKey.Teams_Observe));
        return await ChallengeService.List(model);
    }

    /// <summary>
    /// Find challenges by user
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    [HttpGet("/api/userchallenges")]
    public async Task<ChallengeOverview[]> ListByUser([FromQuery] ChallengeSearchFilter model)
    {
        var userCanObserve = await _permissionsService.Can(PermissionKey.Teams_Observe);
        // if not sudo or not specified, search use Actor.Id as uid in filtering
        if (!userCanObserve || model.uid.IsEmpty())
            model.uid = Actor.Id;

        return await ChallengeService.ListByUser(model.uid);
    }

    /// <summary>
    /// Find archived challenges
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    [HttpGet("/api/challenges/archived")]
    public async Task<ArchivedChallenge[]> ListArchived([FromQuery] SearchFilter model)
    {
        await Authorize(_permissionsService.Can(PermissionKey.Teams_Observe));
        return await ChallengeService.ListArchived(model);
    }

    private async Task<bool> IsSelf(string playerId)
    {
        return await PlayerService.MapId(playerId) == Actor.Id;
    }
}
