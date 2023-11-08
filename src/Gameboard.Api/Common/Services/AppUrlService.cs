using System;
using System.Linq;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;

namespace Gameboard.Api.Common.Services;

public interface IAppUrlService
{
    string GetBaseUrl();
    string ToAppAbsoluteUrl(string relativeUrl);
}

internal class AppUrlService : IAppUrlService
{
    private readonly IServer _server;

    public AppUrlService(IServer server)
    {
        _server = server;
    }

    public string GetBaseUrl()
    {
        if (_server is not null)
        {
            var addresses = _server.Features.Get<IServerAddressesFeature>();

            var rootUrl = addresses.Addresses.FirstOrDefault(a => a.Contains("https"));
            if (rootUrl.IsEmpty())
                rootUrl = addresses.Addresses.FirstOrDefault();

            if (!rootUrl.IsEmpty())
                return rootUrl;
        }

        throw new AppUrlResolutionException();
    }

    public string ToAppAbsoluteUrl(string relativeUrl)
        => ToAbsoluteUrl(GetBaseUrl(), relativeUrl);

    private string ToAbsoluteUrl(string baseUrl, string relativeUrl)
    {
        // if you just convert both the base and the relative url to Uri objects, the `new Uri(baseUri, relativeUri)` ctor
        // does some pretty surprising things (e.g. drops the base path of the base Uri and replaces it with the relative path 
        // of the second). We have to build it manually, unfortunately.
        var finalBaseUrl = baseUrl.TrimEnd('/');

        if (relativeUrl.IsEmpty())
            return finalBaseUrl;

        return $"{finalBaseUrl}/{relativeUrl.TrimStart('/')}";
    }
}
