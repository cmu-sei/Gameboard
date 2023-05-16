// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

using Gameboard.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Caching.Distributed;
using Gameboard.Api.Validators;
using Microsoft.AspNetCore.Http;
using System.IO;
using Gameboard.Api.Features.Games;

namespace Gameboard.Api.Controllers
{
    [Authorize]
    public class SponsorController : _Controller
    {
        private readonly ILogger<SponsorController> _logger;
        SponsorService SponsorService { get; }
        public CoreOptions Options { get; }

        public SponsorController(
            ILogger<SponsorController> logger,
            IDistributedCache cache,
            SponsorValidator validator,
            SponsorService sponsorService,
            CoreOptions options
        ) : base(logger, cache, validator)
        {
            _logger = logger;
            SponsorService = sponsorService;
            Options = options;
        }

        /// <summary>
        /// Create new sponsor
        /// </summary>
        /// <param name="model"></param>
        /// <returns>Sponsor</returns>
        [HttpPost("api/sponsor")]
        [Authorize(Policy = AppConstants.RegistrarPolicy)]
        public async Task<Sponsor> Create([FromBody] NewSponsor model)
        {
            await Validate(model);

            model.Approved = Actor.IsRegistrar;

            return await SponsorService.Create(model);
        }

        [HttpPost("api/sponsors")]
        [Authorize(Policy = AppConstants.RegistrarPolicy)]
        public async Task CreateBatch([FromBody] ChangedSponsor[] model)
        {
            foreach (var s in model)
            {
                s.Approved = Actor.IsRegistrar;

                await SponsorService.AddOrUpdate(s);
            }
        }

        /// <summary>
        /// Retrieve sponsor
        /// </summary>
        /// <param name="id"></param>
        /// <returns>Sponsor</returns>
        [HttpGet("api/sponsor/{id}")]
        [Authorize]
        public async Task<Sponsor> Retrieve([FromRoute] string id)
        {
            return await SponsorService.Retrieve(id);
        }

        /// <summary>
        /// Change sponsor
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPut("api/sponsor")]
        [Authorize(Policy = AppConstants.RegistrarPolicy)]
        public async Task Update([FromBody] ChangedSponsor model)
        {
            await Validate(model);

            model.Approved = Actor.IsRegistrar;

            await SponsorService.AddOrUpdate(model);
        }

        /// <summary>
        /// Delete sponsor
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpDelete("/api/sponsor/{id}")]
        [Authorize(Policy = AppConstants.RegistrarPolicy)]
        public async Task Delete([FromRoute] string id)
        {
            await SponsorService.Delete(id);
        }

        /// <summary>
        /// Find sponsors
        /// </summary>
        /// <param name="model">DataFilter</param>
        /// <returns>Sponsor[]</returns>
        [HttpGet("/api/sponsors")]
        [Authorize]
        public async Task<Sponsor[]> List([FromQuery] SearchFilter model)
        {
            return await SponsorService.List(model);
        }

        [HttpPost("api/sponsor/image")]
        [Authorize(AppConstants.RegistrarPolicy)]
        public async Task<ActionResult<Sponsor>> UploadImage(IFormFile file)
        {
            AuthorizeAny(
                () => Actor.IsRegistrar
            );

            string filename = file.FileName.ToLower();

            string path = Path.Combine(Options.ImageFolder, filename);

            using (var stream = new FileStream(path, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            await SponsorService.AddOrUpdate(
                Path.GetFileNameWithoutExtension(filename),
                filename
            );

            return Ok(new UploadedFile { Filename = filename });
        }
    }
}
