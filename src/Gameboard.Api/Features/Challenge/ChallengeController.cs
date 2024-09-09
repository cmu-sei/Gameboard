// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Challenges;
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

namespace Gameboard.Api.Controllers
{
    [Authorize]
    public class ChallengeController : _Controller
    {
        private readonly IMediator _mediator;
        private readonly IUserRolePermissionsService _permissionsService;

        ChallengeService ChallengeService { get; }
        PlayerService PlayerService { get; }
        IHubContext<AppHub, IAppHubEvent> Hub { get; }
        ConsoleActorMap ActorMap { get; }

        public ChallengeController(
            IActingUserService actingUserService,
            ILogger<ChallengeController> logger,
            IDistributedCache cache,
            ChallengeValidator validator,
            ChallengeService challengeService,
            IMediator mediator,
            IUserRolePermissionsService permissionsService,
            PlayerService playerService,
            IHubContext<AppHub, IAppHubEvent> hub,
            ConsoleActorMap actormap
        ) : base(actingUserService, logger, cache, validator)
        {
            ChallengeService = challengeService;
            PlayerService = playerService;
            Hub = hub;
            _mediator = mediator;
            _permissionsService = permissionsService;
            ActorMap = actormap;
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
            await AuthorizeAny
            (
                _permissionsService.Can(PermissionKey.Teams_DeployGameResources),
                IsSelf(model.PlayerId)
            );

            await Validate(model);

            if (!await _permissionsService.Can(PermissionKey.Play_ChooseChallengeVariant))
                model.Variant = 0;

            string graderUrl = string.Format(
                "{0}://{1}{2}",
                Request.Scheme,
                Request.Host,
                Url.Action("Grade")
            );

            var result = await ChallengeService.GetOrCreate(model, Actor.Id, graderUrl);

            await Hub.Clients.Group(result.TeamId).ChallengeEvent(
                new HubEvent<Challenge>
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
        [Authorize]
        public async Task<Challenge> Retrieve([FromRoute] string id)
        {
            await AuthorizeAny
            (
                _permissionsService.Can(PermissionKey.Teams_Observe),
                ChallengeService.UserIsTeamPlayer(id, Actor.Id)
            );

            await Validate(new Entity { Id = id });

            return await ChallengeService.Retrieve(id);
        }

        /// <summary>
        /// Retrieve challenge preview
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost("api/challenge/preview")]
        [Authorize]
        public async Task<Challenge> Preview([FromBody] NewChallenge model)
        {
            await AuthorizeAny(IsSelf(model.PlayerId));
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
        [Authorize]
        public async Task<Challenge> StartGamespace([FromBody] ChangedChallenge model, CancellationToken cancellationToken)
        {
            await AuthorizeAny
            (
                _permissionsService.Can(PermissionKey.Teams_DeployGameResources),
                ChallengeService.UserIsTeamPlayer(model.Id, Actor.Id)
            );

            await Validate(model);

            var result = await ChallengeService.StartGamespace(model.Id, Actor.Id, cancellationToken);

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
        /// Stop a challenge gamespace
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPut("/api/challenge/stop")]
        [Authorize]
        public async Task<Challenge> StopGamespace([FromBody] ChangedChallenge model)
        {
            await AuthorizeAny
            (
                _permissionsService.Can(PermissionKey.Teams_DeployGameResources),
                ChallengeService.UserIsTeamPlayer(model.Id, Actor.Id)
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
                Task.FromResult(AuthenticatedGraderForChallengeId == model.Id),
                // these are set if the caller authenticated with standard JWT
                _permissionsService.Can(PermissionKey.Teams_DeployGameResources),
                ChallengeService.UserIsTeamPlayer(model.Id, Actor.Id)
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
        [Authorize]
        public async Task<Challenge> Regrade([FromBody] Entity model)
        {
            await AuthorizeAny(_permissionsService.Can(PermissionKey.Scores_RegradeAndRerank));

            await Validate(model);

            var result = await ChallengeService.Regrade(model.Id);

            await Hub.Clients.Group(result.TeamId).ChallengeEvent(
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
        [Authorize]
        public async Task<IEnumerable<GameEngineSectionSubmission>> Audit([FromRoute] string id)
        {
            await AuthorizeAny(_permissionsService.Can(PermissionKey.Teams_Observe));

            await Validate(new Entity { Id = id });

            return await ChallengeService.Audit(id);
        }

        /// <summary>
        /// Console action (ticket, reset)
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost("/api/challenge/console")]
        [Authorize(AppConstants.ConsolePolicy)]
        public async Task<ConsoleSummary> GetConsole([FromBody] ConsoleRequest model)
        {
            await Validate(new Entity { Id = model.SessionId });
            var isTeamMember = await ChallengeService.UserIsTeamPlayer(model.SessionId, Actor.Id);
            Logger.LogInformation($"Console access attempt ({model.Id} / {Actor.Id}): User {Actor.Id}, roles {Actor.Role}, on team = {isTeamMember} .");

            await AuthorizeAny
            (
                _permissionsService.Can(PermissionKey.Teams_Observe),
                Task.FromResult(isTeamMember)
            );

            Logger.LogInformation($"""Console access attempt ({model.Id} / {Actor.Id}): Allowed.""");
            var result = await ChallengeService.GetConsole(model, isTeamMember.Equals(false));

            if (isTeamMember)
                ActorMap.Update(await ChallengeService.SetConsoleActor(model, Actor.Id, Actor.ApprovedName));

            return result;
        }

        /// <summary>
        /// Console action (ticket, reset)
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPut("/api/challenge/console")]
        [Authorize(AppConstants.ConsolePolicy)]
        public async Task SetConsoleActor([FromBody] ConsoleRequest model)
        {
            await Validate(new Entity { Id = model.SessionId });

            var isTeamMember = await ChallengeService.UserIsTeamPlayer(model.SessionId, Actor.Id);

            if (isTeamMember)
                ActorMap.Update(
                    await ChallengeService.SetConsoleActor(model, Actor.Id, Actor.ApprovedName)
                );
        }

        [HttpGet("/api/challenge/consoles")]
        [Authorize]
        public async Task<List<ObserveChallenge>> FindConsoles([FromQuery] string gid)
        {
            await AuthorizeAny(_permissionsService.Can(PermissionKey.Teams_Observe));
            return await ChallengeService.GetChallengeConsoles(gid);
        }

        [HttpGet("/api/challenge/consoleactors")]
        [Authorize]
        public async Task<ConsoleActor[]> GetConsoleActors([FromQuery] string gid)
        {
            await AuthorizeAny(_permissionsService.Can(PermissionKey.Teams_Observe));
            return ChallengeService.GetConsoleActors(gid);
        }

        [HttpGet("/api/challenge/consoleactor")]
        [Authorize(AppConstants.ConsolePolicy)]
        public async Task<ConsoleActor> GetConsoleActor([FromQuery] string uid)
        {
            await AuthorizeAny(_permissionsService.Can(PermissionKey.Teams_Observe));
            return ChallengeService.GetConsoleActor(uid);
        }

        [HttpGet("api/challenge/{challengeId}/solution-guide")]
        [Authorize]
        public Task<ChallengeSolutionGuide> GetSolutionGuide([FromRoute] string challengeId)
            => _mediator.Send(new GetChallengeSolutionGuideQuery(challengeId));

        [HttpGet("api/challenge/{challengeId}/submissions")]
        [Authorize]
        public Task<GetChallengeSubmissionsResponse> GetSubmissions([FromRoute] string challengeId)
            => _mediator.Send(new GetChallengeSubmissionsQuery(challengeId));

        [HttpPut("api/challenge/{challengeId}/submissions/pending")]
        [Authorize]
        public Task UpdatePendingSubmission([FromRoute] string challengeId, [FromBody] ChallengeSubmissionAnswers submission)
            => _mediator.Send(new SaveChallengePendingSubmissionCommand(challengeId, submission));

        /// <summary>
        /// Find challenges
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpGet("/api/challenges")]
        [Authorize]
        public async Task<ChallengeSummary[]> List([FromQuery] SearchFilter model)
        {
            await AuthorizeAny(_permissionsService.Can(PermissionKey.Teams_Observe));

            return await ChallengeService.List(model);
        }

        /// <summary>
        /// Find challenges by user
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpGet("/api/userchallenges")]
        [Authorize]
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
        [Authorize]
        public async Task<ArchivedChallenge[]> ListArchived([FromQuery] SearchFilter model)
        {
            await AuthorizeAny(_permissionsService.Can(PermissionKey.Teams_Observe));
            return await ChallengeService.ListArchived(model);
        }

        private async Task<bool> IsSelf(string playerId)
        {
            return await PlayerService.MapId(playerId) == Actor.Id;
        }
    }
}
