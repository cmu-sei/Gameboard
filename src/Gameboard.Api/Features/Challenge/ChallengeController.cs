// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Gameboard.Api.Features.GameEngine;
using Gameboard.Api.Hubs;
using Gameboard.Api.Services;
using Gameboard.Api.Validators;
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
        ChallengeService ChallengeService { get; }
        PlayerService PlayerService { get; }
        IHubContext<AppHub, IAppHubEvent> Hub { get; }
        ConsoleActorMap ActorMap { get; }

        public ChallengeController(
            ILogger<ChallengeController> logger,
            IDistributedCache cache,
            ChallengeValidator validator,
            ChallengeService challengeService,
            PlayerService playerService,
            IHubContext<AppHub, IAppHubEvent> hub,
            ConsoleActorMap actormap
        ) : base(logger, cache, validator)
        {
            ChallengeService = challengeService;
            PlayerService = playerService;
            Hub = hub;
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
            AuthorizeAny(
                () => Actor.IsDirector,
                () => IsSelf(model.PlayerId).Result
            );

            await Validate(model);

            if (Actor.IsTester.Equals(false))
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
                    ActingUser = HubEventActingUserDescription.FromUser(Actor)
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
            AuthorizeAny(
                () => Actor.IsDirector,
                () => ChallengeService.UserIsTeamPlayer(id, Actor.Id).Result
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
            AuthorizeAny(
                () => IsSelf(model.PlayerId).Result
            );

            await Validate(model);

            return await ChallengeService.Preview(model);
        }

        /// <summary>
        /// Change challenge
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPut("api/challenge")]
        [Authorize]
        public Task Update([FromBody] ChangedChallenge model)
        {
            // await ChallengeService.Update(model);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Delete challenge
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpDelete("/api/challenge/{id}")]
        [Authorize]
        public async Task Delete([FromRoute] string id)
        {
            AuthorizeAny(
                () => Actor.IsDirector
                // () => Actor.IsTester && ChallengeService.UserIsTeamPlayer(id, Actor.Id).Result
            );

            await Validate(new Entity { Id = id });

            await ChallengeService.Delete(id);
            return;
        }

        /// <summary>
        /// Start a  challenge gamespace
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPut("/api/challenge/start")]
        [Authorize]
        public async Task<Challenge> StartGamespace([FromBody] ChangedChallenge model)
        {
            AuthorizeAny(
                () => Actor.IsDirector,
                () => ChallengeService.UserIsTeamPlayer(model.Id, Actor.Id).Result
            );

            await Validate(model);

            var result = await ChallengeService.StartGamespace(model.Id, Actor.Id);

            await Hub.Clients.Group(result.TeamId).ChallengeEvent(
                new HubEvent<Challenge>
                {
                    Model = result,
                    Action = EventAction.Updated,
                    ActingUser = HubEventActingUserDescription.FromUser(Actor)
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
            AuthorizeAny(
                () => Actor.IsDirector,
                () => ChallengeService.UserIsTeamPlayer(model.Id, Actor.Id).Result
            );

            await Validate(new Entity { Id = model.Id });

            var result = await ChallengeService.StopGamespace(model.Id, Actor.Id);

            await Hub.Clients.Group(result.TeamId).ChallengeEvent(
                new HubEvent<Challenge>
                {
                    Model = result,
                    Action = EventAction.Updated,
                    ActingUser = HubEventActingUserDescription.FromUser(Actor)
                }
            );

            return result;
        }

        /// <summary>
        /// Grade a challenge
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPut("/api/challenge/grade")]
        [Authorize(AppConstants.GraderPolicy)]
        public async Task<Challenge> Grade([FromBody] GameEngineSectionSubmission model)
        {
            AuthorizeAny(
                // this is set by _Controller if the caller authenticated with a grader key
                () => AuthenticatedGraderForChallengeId == model.Id,
                // these are set if the caller authenticated with standard JWT
                () => Actor.IsDirector,
                () => ChallengeService.UserIsTeamPlayer(model.Id, Actor.Id).Result
            );

            await Validate(new Entity { Id = model.Id });

            var result = await ChallengeService.Grade(model, Actor.Id);

            await Hub.Clients.Group(result.TeamId).ChallengeEvent(
                new HubEvent<Challenge>
                {
                    Model = result,
                    Action = EventAction.Updated,
                    ActingUser = HubEventActingUserDescription.FromUser(Actor)
                }
            );

            return result;
        }

        /// <summary>
        /// ReGrade a challenge
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPut("/api/challenge/regrade")]
        [Authorize]
        public async Task<Challenge> Regrade([FromBody] Entity model)
        {
            AuthorizeAny(
                () => Actor.IsDirector
            );

            await Validate(model);

            var result = await ChallengeService.Regrade(model.Id);

            await Hub.Clients.Group(result.TeamId).ChallengeEvent(
                new HubEvent<Challenge>
                {
                    Model = result,
                    Action = EventAction.Updated,
                    ActingUser = HubEventActingUserDescription.FromUser(Actor)
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
            AuthorizeAny(
                () => Actor.IsDirector
            );

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

            AuthorizeAny(
              () => Actor.IsDirector,
              () => Actor.IsObserver,
              () => Actor.IsSupport,
              () => isTeamMember
            );

            var result = await ChallengeService.GetConsole(model, isTeamMember.Equals(false));

            if (isTeamMember)
                ActorMap.Update(
                    await ChallengeService.SetConsoleActor(model, Actor.Id, Actor.ApprovedName)
                );

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
            AuthorizeAny(
              () => Actor.IsDirector,
              () => Actor.IsObserver,
              () => Actor.IsSupport
            );
            return await ChallengeService.GetChallengeConsoles(gid);
        }

        [HttpGet("/api/challenge/consoleactors")]
        [Authorize]
        public ConsoleActor[] GetConsoleActors([FromQuery] string gid)
        {
            AuthorizeAny(
              () => Actor.IsDirector,
              () => Actor.IsObserver,
              () => Actor.IsSupport
            );
            return ChallengeService.GetConsoleActors(gid);
        }

        [HttpGet("/api/challenge/consoleactor")]
        [Authorize(AppConstants.ConsolePolicy)]
        public ConsoleActor GetConsoleActor([FromQuery] string uid)
        {
            AuthorizeAny(
              () => Actor.IsDirector,
              () => Actor.IsObserver,
              () => Actor.IsSupport
            );
            return ChallengeService.GetConsoleActor(uid);
        }

        /// <summary>
        /// Find challenges
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpGet("/api/challenges")]
        [Authorize]
        public async Task<ChallengeSummary[]> List([FromQuery] SearchFilter model)
        {
            AuthorizeAny(
                () => Actor.IsDirector,
                () => Actor.IsSupport
            );

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

            // if not sudo or not specified, search use Actor.Id as uid in filtering
            if (!(Actor.IsSupport || Actor.IsObserver) || model.uid.IsEmpty())
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
            AuthorizeAny(
                () => Actor.IsDirector,
                () => Actor.IsSupport
            );

            return await ChallengeService.ListArchived(model);
        }

        private async Task<bool> IsSelf(string playerId)
        {
            return await PlayerService.MapId(playerId) == Actor.Id;
        }
    }
}
