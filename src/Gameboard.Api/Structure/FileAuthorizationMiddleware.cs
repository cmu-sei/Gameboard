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
                var requestPath = context.Request.Path.ToString();
                Regex pattern = new Regex($@"{_options.SupportUploadsRequestPath}/tickets/(?<ticketId>.*)/.*");
                Match match = pattern.Match(requestPath);
                var ticketId = match.Groups["ticketId"].Value;
                var userId = context.User.FindFirst("sub");
                // var userId = _user.FindFirst("sub");
                var key = $"{"file-permit:"}{userId}:{ticketId}";
                string cachedValue = await _cache.GetStringAsync(key);
                if (false && cachedValue.IsEmpty())
                {
                    // throw new Exception("Unauthorized");
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return;
                }
                // if (!context.User.Identity.IsAuthenticated
                //     && context.Request.Path.StartsWithSegments("/"+_options.SupportUploadsRequestPath))
                // {
                //     throw new Exception("Not authenticated");
                // }\
                // context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                // return System.Threading.Tasks.Task.CompletedTask;
                
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
