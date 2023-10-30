using System;
using System.Linq;
using System.Threading;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;

namespace Gameboard.Api.Features.Challenges;

public interface IChallengeGraderUrlService
{
    public string BuildGraderUrl();
}

internal class ChallengeGraderUrlService : IChallengeGraderUrlService
{
    private readonly HttpContext _httpContext;
    private readonly ILogger<ChallengeGraderUrlService> _logger;
    private readonly LinkGenerator _linkGenerator;
    private readonly IServer _server;

    public ChallengeGraderUrlService
    (
        IHttpContextAccessor httpContextAccessor,
        LinkGenerator linkGenerator,
        ILogger<ChallengeGraderUrlService> logger,
        IServer server
    )
    {
        _httpContext = httpContextAccessor.HttpContext;
        _linkGenerator = linkGenerator;
        _logger = logger;
        _server = server;
    }

    public string BuildGraderUrl()
    {
        // prefer to get this from the current request, but it can be null if the call
        // is coming from a background service that runs without an active request.
        // weirdly, HttpContext has a RequestAborted property of type CancellationToken,
        // but that property isn't accessible if the HttpContext has been disposed.
        // So we try/catch. If the context is disposed, we'll get an ObjectDisposed
        // exception, but we catch all exceptions anyway in case we can recover
        // with the server.Features.Get strategy.
        try
        {
            return string.Join
            (
                '/',
                _linkGenerator.GetUriByAction
                (
                    _httpContext,
                    "Grade",
                    "Challenge",
                    null,
                    _httpContext.Request.Scheme,
                    _httpContext.Request.Host,
                    _httpContext.Request.PathBase
                )
            );
        }
        catch (ObjectDisposedException ex)
        {
            _logger.LogInformation($"Attempt to build grader URL with HttpContextAccessor failed: {ex.GetType().Name} :: {ex.Message} Attempting backup strategy...");

            if (_server is not null)
            {
                var addresses = _server.Features.Get<IServerAddressesFeature>();

                var rootUrl = addresses.Addresses.FirstOrDefault(a => a.Contains("https"));
                if (rootUrl.IsEmpty())
                    rootUrl = addresses.Addresses.FirstOrDefault();

                if (!rootUrl.IsEmpty())
                    return $"{rootUrl}/challenge/grade";
            }
        }

        throw new GraderUrlResolutionError();
    }
}
