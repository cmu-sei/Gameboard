// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Features.games;
using Gameboard.Api.Features.Games;
using Gameboard.Api.Features.Games.Start;
using Gameboard.Api.Features.Scores;
using Gameboard.Api.Features.Users;
using Gameboard.Api.Services;
using Gameboard.Api.Validators;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Gameboard.Api.Controllers
{
    [Authorize]
    public class GameController(
        IActingUserService actingUserService,
        ILogger<GameController> logger,
        IDistributedCache cache,
        GameService gameService,
        IScoreDenormalizationService scoreDenormalization,
        GameValidator validator,
        CoreOptions options,
        IMediator mediator,
        IHostEnvironment env,
        IUserRolePermissionsService permissionsService
        ) : GameboardLegacyController(actingUserService, logger, cache, validator)
    {
        GameService GameService { get; } = gameService;
        public CoreOptions Options { get; } = options;
        public IHostEnvironment Env { get; } = env;

        private readonly IMediator _mediator = mediator;
        private readonly IUserRolePermissionsService _permissionsService = permissionsService;
        private readonly IScoreDenormalizationService _scoreDenormalization = scoreDenormalization;

        /// <summary>
        /// Create new game
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost("api/game")]
        public async Task<Game> Create([FromBody] NewGame model)
        {
            await Authorize(_permissionsService.Can(PermissionKey.Games_CreateEditDelete));
            return await GameService.Create(model);
        }

        /// <summary>
        /// Retrieve game
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("api/game/{id}")]
        [AllowAnonymous]
        public async Task<Game> Retrieve([FromRoute] string id)
            => await GameService.Retrieve(id, await _permissionsService.Can(PermissionKey.Games_ViewUnpublished));

        [HttpGet("api/game/{id}/specs")]
        [Authorize]
        public async Task<ChallengeSpec[]> GetChallengeSpecs([FromRoute] string id)
        {
            await Validate(new Entity { Id = id });
            return await GameService.RetrieveChallengeSpecs(id);
        }

        [HttpGet("api/game/{id}/sessions")]
        [Authorize]
        public async Task<SessionForecast[]> GetSessionForecast([FromRoute] string id)
        {
            await Validate(new Entity { Id = id });
            return await GameService.SessionForecast(id);
        }

        [HttpPost("api/game/{gameId}/resources")]
        [Authorize]
        public Task DeployResources([FromRoute] string gameId, [FromBody] DeployGameResourcesBody body)
            => _mediator.Send(new DeployGameResourcesCommand(gameId, body?.TeamIds));

        /// <summary>
        /// Change game
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPut("api/game")]
        public async Task<Data.Game> Update([FromBody] ChangedGame model)
        {
            await _permissionsService.Can(PermissionKey.Games_CreateEditDelete);
            await Validate(model);
            return await GameService.Update(model);
        }

        /// <summary>
        /// Delete game
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpDelete("/api/game/{id}")]
        public Task Delete([FromRoute] string id)
            => _mediator.Send(new DeleteGameCommand(id));

        /// <summary>
        /// Find games
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpGet("/api/games")]
        [AllowAnonymous]
        public async Task<IEnumerable<Game>> List([FromQuery] GameSearchFilter model)
            => await GameService.List(model, await _permissionsService.Can(PermissionKey.Games_ViewUnpublished));

        /// <summary>
        /// List games grouped by year and month
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpGet("/api/games/grouped")]
        [AllowAnonymous]
        public async Task<GameGroup[]> ListGrouped([FromQuery] GameSearchFilter model)
            => await GameService.ListGrouped(model, await _permissionsService.Can(PermissionKey.Games_ViewUnpublished));

        [HttpGet("/api/game/{gameId}/ready")]
        [Authorize]
        public Task<SyncStartState> GetSyncStartState(string gameId)
            => _mediator.Send(new GetSyncStartStateQuery(gameId, Actor));

        [HttpGet("/api/game/{gameId}/play-state")]
        [Authorize]
        public Task<GamePlayState> GetGamePlayState(string gameId)
            => _mediator.Send(new GetGamePlayStateQuery(gameId, Actor.Id));

        [HttpPost("/api/game/import")]
        [Authorize]
        public Task<Game> ImportGameSpec([FromBody] GameSpecImport model)
            => GameService.Import(model);

        [HttpPost("/api/game/export")]
        public async Task<string> ExportGameSpec([FromBody] GameSpecExport model)
        {
            await _permissionsService.Can(PermissionKey.Games_CreateEditDelete);
            return await GameService.Export(model);
        }

        [HttpGet("/api/game/{gameId}/team/{teamId}/gamespace-limit")]
        public Task<TeamGamespaceLimitState> GetTeamGamespaceLimitState([FromRoute] string gameId, [FromRoute] string teamId)
            => _mediator.Send(new GetTeamGamespaceLimitStateQuery(gameId, teamId, Actor));

        [HttpPost("api/game/{id}/card")]
        public async Task<ActionResult<UploadedFile>> UploadGameCard(string id, IFormFile file)
        {
            await Authorize(_permissionsService.Can(PermissionKey.Games_CreateEditDelete));
            return Ok(await GameService.SaveGameCardImage(id, file));
        }

        [HttpPost("api/game/{id}/{type}")]
        public async Task<ActionResult<UploadedFile>> UploadMapImage(string id, string type, IFormFile file)
        {
            await Authorize(_permissionsService.Can(PermissionKey.Games_CreateEditDelete));
            await Validate(new Entity { Id = id });

            string filename = $"{type}_{new Random().Next().ToString("x8")}{Path.GetExtension(file.FileName)}".ToLower();
            string path = Path.Combine(Options.ImageFolder, filename);

            using (var stream = new FileStream(path, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            await GameService.UpdateImage(id, type, filename);

            return Ok(new UploadedFile { Filename = filename });
        }

        [HttpDelete("api/game/{id}/card")]
        public async Task DeleteGameCard([FromRoute] string id)
        {
            await Authorize(_permissionsService.Can(PermissionKey.Games_CreateEditDelete));
            await GameService.DeleteGameCardImage(id);
        }

        [HttpDelete("api/game/{id}/{type}")]
        [Authorize]
        public async Task<ActionResult<UploadedFile>> DeleteImage([FromRoute] string id, [FromRoute] string type)
        {
            await Authorize(_permissionsService.Can(PermissionKey.Games_CreateEditDelete));
            await Validate(new Entity { Id = id });

            string target = $"{id}_{type}.*".ToLower();
            var file = Directory.GetFiles(Options.ImageFolder, target).FirstOrDefault();

            if (file.NotEmpty())
            {
                System.IO.File.Delete(file);
                await GameService.UpdateImage(id, type, "");
            }

            return Ok(new UploadedFile { Filename = "" });
        }

        /// <summary>
        /// Rerank a game's players
        /// </summary>
        /// <param name="id">id</param>
        /// <param name="cancellationToken">id</param>
        /// <returns></returns>
        [HttpPost("/api/game/{id}/rerank")]
        public async Task Rerank([FromRoute] string id, CancellationToken cancellationToken)
        {
            await Authorize(_permissionsService.Can(PermissionKey.Scores_RegradeAndRerank));
            await Validate(new Entity { Id = id });

            await GameService.ReRank(id);
            await _scoreDenormalization.DenormalizeGame(id, cancellationToken);
            await _mediator.Publish(new GameCacheInvalidateNotification(id), cancellationToken);
        }
    }
}
