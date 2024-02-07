using System;
using Gameboard.Api.Common.Services;

namespace Gameboard.Api.Features.GameEngine;

public interface IVmUrlResolver
{
    string ResolveUrl(GameEngineVmState vmState);
}

internal class GameboardMksVmUrlResolver : IVmUrlResolver
{
    private readonly IAppUrlService _appUrlService;

    public GameboardMksVmUrlResolver(IAppUrlService appUrlService)
    {
        _appUrlService = appUrlService;
    }

    public string ResolveUrl(GameEngineVmState vmState)
    {
        var url = _appUrlService.ToAppAbsoluteUrl("mks");
        var urlBuilder = new UriBuilder(url)
        {
            Query = $"f=1&s={vmState.IsolationId}&v={vmState.Name}"
        };

        // constructing the UrlBuilder this way makes it include the port by default
        // (so don't do that)
        urlBuilder.Port = -1;

        return urlBuilder.ToString();
    }
}
