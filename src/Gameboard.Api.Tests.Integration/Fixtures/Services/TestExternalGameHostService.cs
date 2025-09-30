// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using Gameboard.Api.Features.Games.External;

namespace Gameboard.Api.Tests.Integration.Fixtures;

internal class TestExternalGameHostService : IExternalGameHostService
{
    private readonly IHttpClientFactory _httpClientFactory;

    public TestExternalGameHostService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public Task ExtendTeamSession(string teamId, DateTimeOffset newSessionEnd, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public IQueryable<GetExternalGameHostsResponseHost> GetHosts()
        => Array.Empty<GetExternalGameHostsResponseHost>().AsQueryable();

    public async Task<HttpResponseMessage> PingHost(string hostId, CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient();
        return await client.GetAsync("", cancellationToken);
    }

    public Task<IEnumerable<ExternalGameClientTeamConfig>> StartGame(IEnumerable<string> teamIds, CalculatedSessionWindow sessionWindow, CancellationToken cancellationToken)
    {
        return Task.FromResult(Array.Empty<ExternalGameClientTeamConfig>().AsEnumerable());
    }
}
