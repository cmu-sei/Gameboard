
// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Gameboard.Api.Features.Games;
using Gameboard.Api.Features.Teams;
using Gameboard.Api.Services;
using Gameboard.Api.Validators;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace Gameboard.Api.Controllers
{
    [Authorize]
    public class PlayerController : _Controller
    {
        private readonly IMediator _mediator;
        PlayerService PlayerService { get; }
        IMapper Mapper { get; }
        ITeamService TeamService { get; set; }

        public PlayerController(
            ILogger<PlayerController> logger,
            IDistributedCache cache,
            PlayerValidator validator,
            IMediator mediator,
            PlayerService playerService,
            IMapper mapper,
            ITeamService teamService
        ) : base(logger, cache, validator)
        {
            PlayerService = playerService;
            Mapper = mapper;
            _mediator = mediator;
            TeamService = teamService;
        }

        /// <summary>
        /// Enrolls a user in a game.
        /// </summary>
        /// <param name="model"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>A player record which represents an instance of the user playing a given game.</returns>
        [HttpPost("api/player")]
        [Authorize]
        public async Task<Player> Enroll([FromBody] NewPlayer model, CancellationToken cancellationToken)
        {
            AuthorizeAny
            (
                () => Actor.IsRegistrar,
                () => model.UserId == Actor.Id
            );

            await Validate(model);
            return await PlayerService.Enroll(model, Actor, cancellationToken);
        }

        /// <summary>
        /// Retrieve player
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("api/player/{id}")]
        [Authorize]
        public async Task<Player> Retrieve([FromRoute] string id)
        {
            // TODO: consider appropriate authorization
            // Note: this is essentially a scoreboard entry
            AuthorizeAll();

            await Validate(new Entity { Id = id });

            return await PlayerService.Retrieve(id);
        }

        /// <summary>
        /// Change player
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPut("api/player")]
        [Authorize]
        public async Task<PlayerUpdatedViewModel> Update([FromBody] ChangedPlayer model)
        {
            await Validate(model);

            AuthorizeAny
            (
                () => Actor.IsRegistrar,
                () => IsSelf(model.Id).Result
            );

            var result = await PlayerService.Update(model, Actor, Actor.IsRegistrar);
            return Mapper.Map<PlayerUpdatedViewModel>(result);
        }

        [HttpPut("api/player/{playerId}/ready")]
        [Authorize]
        public Task UpdatePlayerReady([FromRoute] string playerId, [FromBody] PlayerReadyUpdate readyUpdate)
            => _mediator.Send(new UpdatePlayerReadyStateCommand(playerId, readyUpdate.IsReady, Actor));

        [HttpPut("api/player/{playerId}/start")]
        [Authorize]
        public async Task<Player> Start(string playerId)
        {
            AuthorizeAny
            (
                () => Actor.IsRegistrar,
                () => IsSelf(playerId).Result
            );

            var sessionStartRequest = new SessionStartRequest { PlayerId = playerId };
            await Validate(sessionStartRequest);
            return await PlayerService.StartSession(sessionStartRequest, Actor, Actor.IsRegistrar);
        }

        /// <summary>
        /// Delete a player enrollment
        /// </summary>
        /// <param name="playerId"></param>
        /// <param name="asAdmin"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        [HttpDelete("/api/player/{playerId}")]
        [Authorize]
        public async Task Unenroll([FromRoute] string playerId, [FromQuery] bool asAdmin, CancellationToken cancellationToken)
        {
            AuthorizeAny(
                () => Actor.IsRegistrar,
                () => IsSelf(playerId).Result
            );

            var unenrollRequest = new PlayerUnenrollRequest
            {
                Actor = Actor,
                PlayerId = playerId,
                AsAdmin = asAdmin
            };

            await Validate(unenrollRequest);
            await PlayerService.Unenroll(unenrollRequest, cancellationToken);
        }

        /// <summary>
        /// Find players
        /// </summary>
        /// <remarks>
        /// Filter with query params `gid, tid, uid, org` (group, team, user, sponsor ids)
        /// Filter with query param `filter=collapse` to pull just one player record per team.
        /// </remarks>
        /// <param name="model">PlayerDataFilter</param>
        /// <returns></returns>
        [HttpGet("/api/players")]
        [AllowAnonymous]
        public async Task<Player[]> List([FromQuery] PlayerDataFilter model)
        {
            return await PlayerService.List(model, Actor.IsRegistrar);
        }

        /// <summary>
        /// Show scoreboard
        /// </summary>
        /// <remarks>Include querystring value `gid` for game id</remarks>
        /// <param name="model">PlayerDataFilter</param>
        /// <returns>Standings</returns>
        [HttpGet("/api/scores")]
        // [ResponseCache] // TODO: consider response caching for this endpoint
        [AllowAnonymous]
        public async Task<Standing[]> Scores([FromQuery] PlayerDataFilter model)
        {
            return await PlayerService.Standings(model);
        }

        /// <summary>
        /// Load active challenge data for a team.
        /// </summary>
        /// <param name="id">The id of the team who owns the challenges</param>
        /// <returns>An array of challenge entries.</returns>
        [HttpGet("/api/team/{id}/challenges")]
        [Authorize]
        public async Task<IEnumerable<TeamChallenge>> GetTeamChallenges([FromRoute] string id)
        {
            AuthorizeAny
            (
                () => Actor.IsAdmin,
                () => Actor.IsDirector,
                () => Actor.IsObserver
            );

            return await PlayerService.LoadChallengesForTeam(id);
        }

        /// <summary>
        /// Get a Game's TeamSummary
        /// </summary>
        /// <param name="id">Game Id</param>
        /// <returns>TeamSummary[]</returns>
        [HttpGet("/api/teams/{id}")]
        [Authorize]
        public async Task<TeamSummary[]> GetTeams([FromRoute] string id)
        {
            AuthorizeAny(() => Actor.IsRegistrar);

            return await PlayerService.LoadTeams(id, Actor.IsRegistrar);
        }

        /// <summary>
        /// Get a Game's Teams with Members
        /// </summary>
        /// <param name="id">Game Id</param>
        /// <returns>Team[]</returns>
        [HttpGet("/api/teams/observe/{id}")]
        [Authorize]
        public async Task<IEnumerable<ObserveTeam>> ObserveTeams([FromRoute] string id)
        {
            AuthorizeAny(
                () => Actor.IsDirector,
                () => Actor.IsObserver
            );

            return await PlayerService.ObserveTeams(id);
        }

        /// <summary>
        /// Get Player Team
        /// </summary>
        /// <param name="id">player id</param>
        /// <returns>Team</returns>
        [HttpGet("/api/board/{id}")]
        [Authorize]
        public async Task<BoardPlayer> GetBoard([FromRoute] string id)
        {
            await Validate(new Entity { Id = id });
            AuthorizeAny(() => IsSelf(id).Result);

            return await PlayerService.LoadBoard(id);
        }

        /// <summary>
        /// Advance an enrollment to a different game
        /// </summary>
        /// <param name="model">TeamAdvancement</param>
        /// <returns></returns>
        [HttpPost("/api/team/advance")]
        [Authorize(AppConstants.DesignerPolicy)]
        public async Task AdvanceTeams([FromBody] TeamAdvancement model)
        {
            await Validate(model);

            await PlayerService.AdvanceTeams(model);
        }

        [HttpPost("/api/player/{id}/invite")]
        [Authorize]
        public async Task<TeamInvitation> Invite([FromRoute] string id)
        {
            AuthorizeAny(
                () => Actor.IsRegistrar,
                () => IsSelf(id).Result
            );

            await Validate(new Entity { Id = id });

            return await PlayerService.GenerateInvitation(id);
        }

        /// <summary>
        /// Enlists the user into a player team
        /// </summary>
        /// <param name="model">EnlistingPlayer</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        [HttpPost("/api/player/enlist")]
        [Authorize]
        public async Task<Player> Enlist([FromBody] PlayerEnlistment model, CancellationToken cancellationToken)
        {
            AuthorizeAny(
                () => Actor.IsRegistrar,
                () => model.UserId == Actor.Id,
                () => IsSelf(model.PlayerId).Result
            );

            await Validate(model);
            return await PlayerService.Enlist(model, Actor, cancellationToken);
        }

        [HttpPut("/api/team/{teamId}/manager/{playerId}")]
        [Authorize]
        public async Task PromoteToManager(string teamId, string playerId, [FromBody] PromoteToManagerRequest promoteRequest, CancellationToken cancellationToken)
        {
            AuthorizeAny
            (
                () => Actor.IsRegistrar,
                () => PlayerService.Retrieve(promoteRequest.CurrentManagerPlayerId).Result.UserId == Actor.Id
            );

            // TODO: kinda yuck. we're only really counting on the caller for AsAdmin (which we're not using yet)
            // and CurrentManagerPlayerId, and we're ignoring whatever else they pass, but still a bit iffy
            var model = new PromoteToManagerRequest
            {
                Actor = Actor,
                AsAdmin = promoteRequest.AsAdmin,
                CurrentManagerPlayerId = promoteRequest.CurrentManagerPlayerId,
                NewManagerPlayerId = playerId,
                TeamId = teamId
            };

            await Validate(model);
            await TeamService.PromoteCaptain(teamId, playerId, Actor, cancellationToken);
        }

        /// <summary>
        /// Get Player Certificate
        /// </summary>
        /// <param name="id">player id</param>
        /// <returns></returns>
        [HttpGet("/api/certificate/{id}")]
        [Authorize]
        public async Task<PlayerCertificate> GetCertificate([FromRoute] string id)
        {
            await Validate(new Entity { Id = id });

            AuthorizeAny(
                () => IsSelf(id).Result
            );

            return await PlayerService.MakeCertificate(id);
        }

        /// <summary>
        /// Get List of Player Certificates
        /// </summary>
        /// <returns> </returns>
        [HttpGet("/api/certificates")]
        [Authorize]
        public async Task<IEnumerable<PlayerCertificate>> GetCertificates()
        {
            return await PlayerService.MakeCertificates(Actor.Id);
        }

        private async Task<bool> IsSelf(string playerId)
        {
            return await PlayerService.MapId(playerId) == Actor.Id;
        }
    }
}
