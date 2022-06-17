// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Gameboard.Api;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace Gameboard.Api
{
    public class FileAuthorizationMiddleware
    {
        public FileAuthorizationMiddleware(
            RequestDelegate next,
            ILogger<HeaderInspectionMiddleware> logger,
            CoreOptions options,
            IDistributedCache cache
        ){
            _next = next;
            _logger = logger;
            _options = options;
            _cache = cache;
            // _user = user;
        }
        private readonly RequestDelegate _next;
        private readonly ILogger _logger;
        private readonly CoreOptions _options;
        private readonly IDistributedCache _cache;

        public async Task Invoke(HttpContext context)
        {
            if (context.Request.Path.StartsWithSegments("/"+_options.SupportUploadsRequestPath))
            {
                // TODO: May need to change approach for static file requests since the UI with render them with src without token

                // var requestPath = context.Request.Path.ToString();
                // Regex pattern = new Regex($@"{_options.SupportUploadsRequestPath}/(?<ticketId>.*)/.*");
                // Match match = pattern.Match(requestPath);
                // var ticketId = match.Groups["ticketId"].Value;
                // var sub = context.User.FindFirst("sub");
                // if (sub == null)
                // {
                //     context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                //     return;
                // }
                // var userId = context.User.FindFirst("sub").Value;
                // var key = $"{"file-permit:"}{userId}:{ticketId}";
                // string cachedValue = await _cache.GetStringAsync(key);
                // if (cachedValue.IsEmpty())
                // {
                //     context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                //     return;
                // }
            }

            await _next(context);
        }
    }
}

namespace Microsoft.AspNetCore.Builder
{
    public static class FileAuthorizationExtensions
    {
        public static IApplicationBuilder UseFileProtection (
            this IApplicationBuilder builder
        )
        {
            return builder.UseMiddleware<FileAuthorizationMiddleware>();
        }
    }
}
