using System;
using Microsoft.AspNetCore.Http;

namespace Gameboard.Api.Common.Services;

public interface IAppUrlService
{
    string GetBaseUrl();
    string GetAbsoluteUrlFromRelative(string relativeUrl);
}

internal class AppUrlService : IAppUrlService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AppUrlService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string GetBaseUrl()
    {
        var request = _httpContextAccessor.HttpContext.Request;
        var builder = new UriBuilder(request.Scheme, request.Host.Host, request.Host.Port ?? -1, request.PathBase);

        return builder.ToString();
    }

    public string GetAbsoluteUrlFromRelative(string relativeUrl)
    {
        // if you just convert both the base and the relative url to Uri objects, the `new Uri(baseUri, relativeUri)` ctor
        // does some pretty surprising things (e.g. drops the base path of the base Uri and replaces it with the relative path 
        // of the second). We have to build it manually, unfortunately.
        var baseUrl = GetBaseUrl().TrimEnd('/');

        if (relativeUrl.IsEmpty())
            return baseUrl;

        return $"{baseUrl}/{relativeUrl.TrimStart('/')}";
    }
}
