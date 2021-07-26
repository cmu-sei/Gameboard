// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

﻿using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

using Gameboard.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Caching.Distributed;
using Gameboard.Api.Validators;
using Microsoft.AspNetCore.Http;
using System.IO;
using Microsoft.Extensions.Hosting;
using System.Linq;

namespace Gameboard.Api.Controllers
{
    [Authorize]
    public class GameController : _Controller
    {
        GameService GameService { get; }
        public CoreOptions Options { get; }
        public IHostEnvironment Env { get; }

        public GameController(
            ILogger<GameController> logger,
            IDistributedCache cache,
            GameService gameService,
            GameValidator validator,
            CoreOptions options,
            IHostEnvironment env
        ) : base(logger, cache, validator)
        {
            GameService = gameService;
            Options = options;
            Env = env;
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
            return await GameService.Retrieve(id);
        }

        [HttpGet("api/game/{id}/specs")]
        [Authorize]
        public async Task<ChallengeSpec[]> RetrieveChallenges([FromRoute] string id)
        {
            await Validate(new Entity{ Id = id });

            return await GameService.RetrieveChallenges(id);
        }

        [HttpGet("api/game/{id}/sessions")]
        [Authorize]
        public async Task<SessionForecast[]> GetSessionForecast([FromRoute] string id)
        {
            await Validate(new Entity{ Id = id });

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
            await Validate(new Entity{ Id = model.Id });

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
            await Validate(new Entity{ Id = id });

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
            return await GameService.List(model, Actor.IsDesigner);
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

            await Validate(new Entity{ Id = id });

            string filename = $"{id}_{type}{Path.GetExtension(file.FileName)}".ToLower();

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
        public async Task<ActionResult<UploadedFile>> DeleteImage([FromRoute]string id, [FromRoute]string type)
        {
            AuthorizeAny(
                () => Actor.IsDesigner
            );

            await Validate(new Entity{ Id = id });

            string target = $"{id}_{type}.*".ToLower();

            var file = Directory.GetFiles(Options.ImageFolder, target).FirstOrDefault();

            if (file.NotEmpty())
            {
                System.IO.File.Delete(file);

                await GameService.UpdateImage(id, type, "");

            }

            return Ok(new UploadedFile { Filename = "" });
        }

    }
}
