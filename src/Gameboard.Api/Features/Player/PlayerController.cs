// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Gameboard.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Caching.Distributed;
using Gameboard.Api.Validators;
using Microsoft.AspNetCore.SignalR;
using Gameboard.Api.Hubs;
using AutoMapper;

namespace Gameboard.Api.Controllers
{
    [Authorize]
    public class PlayerController : _Controller
    {
        PlayerService PlayerService { get; }
        IHubContext<AppHub, IAppHubEvent> Hub { get; }
        IMapper Mapper { get; }

        public PlayerController(
            ILogger<PlayerController> logger,
            IDistributedCache cache,
            PlayerValidator validator,
            PlayerService playerService,
            IHubContext<AppHub, IAppHubEvent> hub,
            IMapper mapper
        ): base(logger, cache, validator)
        {
            PlayerService = playerService;
            Hub = hub;
            Mapper = mapper;
        }

        /// <summary>
        /// Create new player
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost("api/player")]
        [Authorize]
        public async Task<Player> Register([FromBody] NewPlayer model)
        {
            AuthorizeAny(
                () => Actor.IsRegistrar,
                () => model.UserId == Actor.Id
            );

            await Validate(model);

            return await PlayerService.Register(model, Actor.IsRegistrar);
        }

        /// <summary>
        /// Retrieve player
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("api/player/{id}")]
        [Authorize]
        public async Task<Player> Retrieve([FromRoute]string id)
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
        public async Task Update([FromBody] ChangedPlayer model)
        {
            await Validate(model);

            AuthorizeAny(
                () => Actor.IsRegistrar,
                () => PlayerService.MapId(model.Id).Result == Actor.Id
            );

            var result = await PlayerService.Update(model, Actor.IsRegistrar);

            await Hub.Clients.Group(result.TeamId).TeamEvent(
                new HubEvent<TeamState>(Mapper.Map<TeamState>(result), EventAction.Updated)
            );
        }

        /// <summary>
        /// Change player session
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPut("api/team/session")]
        [Authorize]
        public async Task UpdateSession([FromBody] SessionChangeRequest model)
        {
            await Validate(model);

            AuthorizeAny(
                () => Actor.IsRegistrar
            );

            var result = await PlayerService.ExtendSession(model);

            await Hub.Clients.Group(result.TeamId).TeamEvent(
                new HubEvent<TeamState>(Mapper.Map<TeamState>(result), EventAction.Updated)
            );
        }

        /// <summary>
        /// Start player/team session
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPut("api/player/start")]
        [Authorize]
        public async Task<Player> Start([FromBody] SessionStartRequest model)
        {
            AuthorizeAny(
                () => Actor.IsRegistrar,
                () => PlayerService.MapId(model.Id).Result == Actor.Id
            );

            await Validate(model);

            var result = await PlayerService.Start(model, Actor.IsRegistrar);

            await Hub.Clients.Group(result.TeamId).TeamEvent(
                new HubEvent<TeamState>(Mapper.Map<TeamState>(result), EventAction.Started)
            );

            return result;
        }

        /// <summary>
        /// Delete a player enrollment
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpDelete("/api/player/{id}")]
        [Authorize]
        public async Task Delete([FromRoute]string id)
        {
            AuthorizeAny(
                () => Actor.IsRegistrar,
                () => IsSelf(id).Result
            );

            await Validate(new Entity { Id = id });

            var player = await PlayerService.Delete(id, Actor.IsRegistrar);

            await Hub.Clients.Group(player.TeamId).PresenceEvent(
                new HubEvent<TeamPlayer>(Mapper.Map<TeamPlayer>(player), EventAction.Deleted)
            );

            if (player.IsManager)
            {
                await Hub.Clients.Group(player.TeamId).TeamEvent(
                    new HubEvent<TeamState>(
                        new TeamState { TeamId = player.TeamId },
                        EventAction.Deleted
                    )
                );
            }
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
        /// Get Player Team
        /// </summary>
        /// <param name="id"></param>
        /// <returns>Team</returns>
        [HttpGet("/api/team/{id}")]
        [Authorize]
        public async Task<Team> GetTeam([FromRoute] string id)
        {
            return await PlayerService.LoadTeam(id, Actor.IsObserver || Actor.IsDirector);
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
            AuthorizeAny(
                () => Actor.IsRegistrar
            );

            return await PlayerService.LoadTeams(id, Actor.IsRegistrar);
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
            await Validate(new Entity{ Id = id });

            AuthorizeAny(
                () => IsSelf(id).Result
            );

            return await PlayerService.LoadBoard(id);
        }

        /// <summary>
        /// Advance an enrollment to a different game
        /// </summary>
        /// <param name="model">TeamAdvancement</param>
        /// <returns></returns>
        [HttpPost("/api/team/advance")]
        [Authorize(AppConstants.DesignerPolicy)]
        public async Task AdvanceTeams([FromBody]TeamAdvancement model)
        {
            await Validate(model);

            await PlayerService.AdvanceTeams(model);
        }

        [HttpPost("/api/player/{id}/invite")]
        [Authorize]
        public async Task<TeamInvitation> Invite([FromRoute]string id)
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
        /// <returns></returns>
        [HttpPost("/api/player/enlist")]
        [Authorize]
        public async Task<Player> Enlist([FromBody]PlayerEnlistment model)
        {
            AuthorizeAny(
                () => Actor.IsRegistrar,
                () => model.UserId == Actor.Id,
                () => PlayerService.MapId(model.PlayerId).Result == Actor.Id
            );

            await Validate(model);

            return await PlayerService.Enlist(model, Actor.IsRegistrar);
        }

        private async Task<bool> IsSelf(string playerId)
        {
          return await PlayerService.MapId(playerId) == Actor.Id;
        }

        /// <summary>
        /// Rerank a game's players
        /// </summary>
        /// <param name="gameId">id</param>
        /// <returns></returns>
        [HttpPost("/api/player/rerank")]
        [Authorize(AppConstants.AdminPolicy)]
        public async Task Rerank([FromBody]string gameId)
        {
            await PlayerService.ReRank(gameId);
        }

    }
}
