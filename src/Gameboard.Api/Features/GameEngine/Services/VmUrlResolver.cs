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
        var url = _appUrlService.GetAbsoluteUrlFromRelative("mks");
        var urlBuilder = new UriBuilder(url);
        urlBuilder.Query = $"f=1&s={vmState.Id}&v={vmState.Name}";

        return urlBuilder.ToString();
    }
}
