using Gameboard.Api.Features.Games.External;

namespace Gameboard.Api.Tests.Integration.Fixtures;

internal class TestGamebrainService : IExternalGameHostService
{
    private readonly IHttpClientFactory _httpClientFactory;

    public TestGamebrainService(IHttpClientFactory httpClientFactory)
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

    public Task<IEnumerable<ExternalGameClientTeamConfig>> StartGame(ExternalGameStartMetaData metaData, CancellationToken cancellationToken)
    {
        return Task.FromResult(Array.Empty<ExternalGameClientTeamConfig>().AsEnumerable());
    }
}
