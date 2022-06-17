// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

using Gameboard.Api.Services;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.AspNetCore.Authorization;
using Gameboard.Api.Validators;
using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace Gameboard.Api.Controllers
{
    [Authorize]
    public class TicketController : _Controller
    {
        TicketService TicketService { get; }
        public CoreOptions Options { get; }
        

        public TicketController(
            ILogger<ChallengeController> logger,
            IDistributedCache cache,
            TicketValidator validator,
            CoreOptions options,
            TicketService ticketService
        ): base(logger, cache, validator)
        {
            TicketService = ticketService;
            Options = options;
        }

        /// <summary>
        /// Gets ticket details
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("api/ticket/{id}")]
        [Authorize]
        public async Task<Ticket> Retrieve([FromRoute] string id)
        {
            AuthorizeAny(
                () => Actor.IsObserver,
                () => TicketService.IsOwnerOrTeamMember(id, Actor.Id).Result
            );

           await Validate(new Entity { Id = id });

            // Once authenticated, authorized, and validated, cache a file permit for this user id & ticket id
            await Cache.SetStringAsync(
                $"{"file-permit:"}{Actor.Id}:{id}",
                "true",
                new DistributedCacheEntryOptions {
                    AbsoluteExpirationRelativeToNow = new TimeSpan(0, 15, 0)
                }
            );

            return await TicketService.Retrieve(id, Actor.Id);
        }


        /// <summary>
        /// Create new ticket
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost("/api/ticket")]
        [Authorize]
        public async Task<Ticket> Create([FromForm]NewTicket model)
        {

            await Validate(model);

            List<UploadFile> uploads = GetUploadFiles(model.Uploads);

            var result = await TicketService.Create(model, Actor.Id, Actor.IsSupport, uploads);

            if (uploads.Count() > 0 && result != null && !result.Id.IsEmpty())
            {
                string path = BuildPath(result.Id);
                await WriteUploadFiles(uploads, path);
            }

            return result;
        }

        /// <summary>
        /// Update ticket
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPut("/api/ticket")]
        [Authorize]
        public async Task<Ticket> Update([FromBody]ChangedTicket model)
        {
            AuthorizeAny(
                () => Actor.IsSupport,
                () => TicketService.UserCanUpdate(model.Id, Actor.Id).Result
            );

            await Validate(model);

            var result = await TicketService.Update(model, Actor.Id, Actor.IsSupport);

            return result;
        }

        /// <summary>
        /// Lists tickets based on search params
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpGet("/api/ticket/list")]
        [Authorize]
        public async Task<TicketSummary[]> List([FromQuery] TicketSearchFilter model)
        {
            return await TicketService.List(model, Actor.Id, Actor.IsSupport || Actor.IsObserver);
        }

        /// <summary>
        /// Create new ticket comment
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost("/api/ticket/comment")]
        [Authorize]
        public async Task<TicketActivity> AddComment([FromForm]NewTicketComment model)
        {
            AuthorizeAny(
                () => Actor.IsObserver,
                () => Actor.IsSupport,
                () => TicketService.IsOwnerOrTeamMember(model.TicketId, Actor.Id).Result
            );

            await Validate(model);

            List<UploadFile> uploads = GetUploadFiles(model.Uploads);

            var result = await TicketService.AddComment(model, Actor.Id, uploads);

            if (uploads.Count() > 0 && result != null && !result.Id.IsEmpty())
            {
                string path = BuildPath(result.TicketId, result.Id);
                await WriteUploadFiles(uploads, path);
            }

            return result;
        }


        /// <summary>
        /// Lists all distinct labels
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpGet("/api/ticket/labels")]
        [Authorize]
        public async Task<string[]> ListLabels([FromQuery] SearchFilter model)
        {
            AuthorizeAny(
                () => Actor.IsSupport,
                () => Actor.IsObserver
            );

            return await TicketService.ListLabels(model);
        }

        private List<UploadFile> GetUploadFiles(List<IFormFile> uploads)
        {
            List<UploadFile> result = new List<UploadFile>();
            if (uploads != null)
            {
                var fileNum = 1;
                foreach (var upload in uploads)
                {
                    string nameOnly = Path.GetFileNameWithoutExtension(upload.FileName).ToLower();
                    string extension = Path.GetExtension(upload.FileName);
                    string filename = $"{nameOnly}_{fileNum}{extension}";
                    var sanitized = filename.SanitizeFilename();
                    result.Add(new UploadFile{ FileName = sanitized, File = upload});
                    fileNum += 1;
                }
            }
            return result;
        }

        private async Task WriteUploadFiles(List<UploadFile> uploads, string path)
        {
            foreach (var upload in uploads)
            {
                string filePath = Path.Combine(path, upload.FileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await upload.File.CopyToAsync(stream);
                }
            }
        }

        private string BuildPath(params string[] segments)
        {
            string path = Options.SupportUploadsFolder;

            foreach (string s in segments)
                path = Path.Combine(path, s);

            if (!System.IO.Directory.Exists(path) && !System.IO.File.Exists(path))
                System.IO.Directory.CreateDirectory(path);

            return path;
        }

    }
}
