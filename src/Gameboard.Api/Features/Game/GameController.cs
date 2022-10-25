// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Gameboard.Api.Services;
using Gameboard.Api.Validators;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Gameboard.Api.Controllers
{
    [Authorize]
    public class GameController : _Controller
    {
        GameService GameService { get; }
        public CoreOptions Options { get; }
        public IHostEnvironment Env { get; }
        private readonly IHttpClientFactory HttpClientFactory;

        public GameController(
            ILogger<GameController> logger,
            IDistributedCache cache,
            GameService gameService,
            GameValidator validator,
            CoreOptions options,
            IHostEnvironment env,
            IHttpClientFactory factory
        ) : base(logger, cache, validator)
        {
            GameService = gameService;
            Options = options;
            Env = env;
            HttpClientFactory = factory;
        }

        /// <summary>
        /// Create new game
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost("api/game")]
        [Authorize(AppConstants.DesignerPolicy)]
        public async Task<Game> Create([FromBody] NewGame model)
        {
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
        {
            // only designers and testers can retrieve or list unpublished games
            return await GameService.Retrieve(id, Actor.IsDesigner || Actor.IsTester);
        }

        [HttpGet("api/game/{id}/specs")]
        [Authorize]
        public async Task<ChallengeSpec[]> RetrieveChallenges([FromRoute] string id)
        {
            await Validate(new Entity { Id = id });

            return await GameService.RetrieveChallenges(id);
        }

        [HttpGet("api/game/{id}/sessions")]
        [Authorize]
        public async Task<SessionForecast[]> GetSessionForecast([FromRoute] string id)
        {
            await Validate(new Entity { Id = id });

            return await GameService.SessionForecast(id);
        }

        /// <summary>
        /// Change game
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPut("api/game")]
        [Authorize(AppConstants.DesignerPolicy)]
        public async Task Update([FromBody] ChangedGame model)
        {
            await Validate(new Entity { Id = model.Id });

            await GameService.Update(model);
        }

        /// <summary>
        /// Delete game
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpDelete("/api/game/{id}")]
        [Authorize(AppConstants.DesignerPolicy)]
        public async Task Delete([FromRoute] string id)
        {
            await Validate(new Entity { Id = id });

            await GameService.Delete(id);
        }

        /// <summary>
        /// Find games
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpGet("/api/games")]
        [AllowAnonymous]
        public async Task<Game[]> List([FromQuery] GameSearchFilter model)
        {
            return await GameService.List(model, Actor.IsDesigner || Actor.IsTester);
        }

        /// <summary>
        /// List games grouped by year and month
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpGet("/api/games/grouped")]
        [AllowAnonymous]
        public async Task<GameGroup[]> ListGrouped([FromQuery] GameSearchFilter model)
        {
            return await GameService.ListGrouped(model, Actor.IsDesigner || Actor.IsTester);
        }

        [HttpPost("/api/game/import")]
        [Authorize(AppConstants.DesignerPolicy)]
        public async Task<Game> ImportGameSpec([FromBody] GameSpecImport model)
        {

            return await GameService.Import(model);
        }

        [HttpPost("/api/game/export")]
        [Authorize(AppConstants.DesignerPolicy)]
        public async Task<string> ExportGameSpec([FromBody] GameSpecExport model)
        {

            return await GameService.Export(model);
        }

        [HttpPost("api/game/{id}/{type}")]
        [Authorize]
        public async Task<ActionResult<UploadedFile>> UploadMapImage(string id, string type, IFormFile file)
        {
            AuthorizeAny(
                () => Actor.IsDesigner
            );

            await Validate(new Entity { Id = id });

            string filename = $"{type}_{(new Random()).Next().ToString("x8")}{Path.GetExtension(file.FileName)}".ToLower();

            string path = Path.Combine(Options.ImageFolder, filename);

            using (var stream = new FileStream(path, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            await GameService.UpdateImage(id, type, filename);

            return Ok(new UploadedFile { Filename = filename });
        }

        [HttpDelete("api/game/{id}/{type}")]
        [Authorize]
        public async Task<ActionResult<UploadedFile>> DeleteImage([FromRoute] string id, [FromRoute] string type)
        {
            AuthorizeAny(
                () => Actor.IsDesigner
            );

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
        /// <returns></returns>
        [HttpPost("/api/game/{id}/rerank")]
        [Authorize(AppConstants.AdminPolicy)]
        public async Task Rerank([FromRoute] string id)
        {
            AuthorizeAny(
                () => Actor.IsDesigner
            );

            await Validate(new Entity { Id = id });

            await GameService.ReRank(id);
        }

        #region GAMEBRAIN METHODS
        [HttpGet("/api/game/headless/{tid}")]
        [Authorize]
        public async Task<string> GetGameUrl([FromQuery] string gid, [FromRoute] string tid)
        {
            AuthorizeAny(
                () => Actor.IsDirector,
                () => GameService.UserIsTeamPlayer(Actor.Id, gid, tid).Result
            );

            var gb = await CreateGamebrain();
            var m = await gb.GetAsync($"admin/headless_client/{tid}");
            return await m.Content.ReadAsStringAsync();
        }

        [HttpGet("/api/deployunityspace/{gid}/{tid}")]
        [Authorize]
        public async Task<string> DeployUnitySpace([FromRoute] string gid, [FromRoute] string tid)
        {
            Console.WriteLine($"Deploy? {gid} is the GID.");

            AuthorizeAny(
                () => Actor.IsDirector,
                () => GameService.UserIsTeamPlayer(Actor.Id, gid, tid).Result
            );

            var gb = await CreateGamebrain();
            var m = await gb.GetAsync($"admin/deploy/{gid}/{tid}");
            return await m.Content.ReadAsStringAsync();
        }

        [HttpGet("/api/getGamespace/{gid}/{tid}")]
        [Authorize]
        public async Task<IActionResult> HasGamespace([FromRoute] string gid, [FromRoute] string tid)
        {
            AuthorizeAny(
                () => GameService.UserIsTeamPlayer(Actor.Id, gid, tid).Result
            );

            var gb = await CreateGamebrain();
            var m = await gb.GetAsync($"team_active/{tid}");

            if (m.IsSuccessStatusCode)
            {
                var stringContent = await m.Content.ReadAsStringAsync();

                if (!stringContent.IsEmpty())
                {
                    return new JsonResult(stringContent);
                }

                return Ok();
            }
            else
            {
                var response = new ObjectResult($"Bad response from Gamebrain: {m.Content} : {m.ReasonPhrase}");
                response.StatusCode = (int)m.StatusCode;
                return response;
            }
        }

        [HttpGet("/api/undeployunityspace/{tid}")]
        [Authorize]
        public async Task<string> UndeployUnitySpace([FromQuery] string gid, [FromRoute] string tid)
        {
            AuthorizeAny(
                () => Actor.IsAdmin,
                () => GameService.UserIsTeamPlayer(Actor.Id, gid, tid).Result
            );

            var accessToken = await HttpContext.GetTokenAsync("access_token");
            HttpClient gb = await CreateGamebrain();

            var m = await gb.GetAsync($"admin/undeploy/{tid}");
            return await m.Content.ReadAsStringAsync();
        }

        private async Task<HttpClient> CreateGamebrain()
        {
            var gb = HttpClientFactory.CreateClient("Gamebrain");
            gb.DefaultRequestHeaders.Add("Authorization", $"Bearer {await HttpContext.GetTokenAsync("access_token")}");
            return gb;
        }
        #endregion
    }
}
