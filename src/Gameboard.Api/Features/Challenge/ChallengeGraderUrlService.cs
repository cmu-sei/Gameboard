using System.Linq;
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
        if (_server is not null)
        {
            var addresses = _server.Features.Get<IServerAddressesFeature>();

            var rootUrl = addresses.Addresses.FirstOrDefault(a => a.Contains("https"));
            if (rootUrl.IsEmpty())
                rootUrl = addresses.Addresses.FirstOrDefault();

            if (!rootUrl.IsEmpty())
                return $"{rootUrl}/challenge/grade";
        }

        throw new GraderUrlResolutionError();
    }
}
