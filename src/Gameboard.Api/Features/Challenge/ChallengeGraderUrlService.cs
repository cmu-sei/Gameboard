using System.Linq;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;

namespace Gameboard.Api.Features.Challenges;

public interface IChallengeGraderUrlService
{
    public string BuildGraderUrl();
}

internal class ChallengeGraderUrlService : IChallengeGraderUrlService
{
    private readonly IServer _server;

    public ChallengeGraderUrlService(IServer server)
    {
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
