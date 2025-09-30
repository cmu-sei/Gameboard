// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using Gameboard.Api.Common.Services;

namespace Gameboard.Api.Features.GameEngine;

public interface IVmUrlResolver
{
    string ResolveUrl(GameEngineVmState vmState);
}

internal class GameboardMksVmUrlResolver(IAppUrlService appUrlService) : IVmUrlResolver
{
    private readonly IAppUrlService _appUrlService = appUrlService;

    public string ResolveUrl(GameEngineVmState vmState)
    {
        var url = _appUrlService.ToAppAbsoluteUrl("mks");
        var urlBuilder = new UriBuilder(url)
        {
            Query = $"f=1&s={vmState.IsolationId}&v={vmState.Name}",
            // constructing the UrlBuilder this way makes it include the port by default
            // (so don't do that)
            Port = -1
        };

        return urlBuilder.ToString();
    }
}
