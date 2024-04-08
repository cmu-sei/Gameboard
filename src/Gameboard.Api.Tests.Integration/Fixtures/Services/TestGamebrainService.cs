using Gameboard.Api.Features.Games.External;

namespace Gameboard.Api.Tests.Integration.Fixtures;

internal class TestGamebrainService : IExternalGameHostService
{
    public Task ExtendTeamSession(string teamId, DateTimeOffset newSessionEnd, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public IQueryable<GetExternalGameHostsResponseHost> GetHosts()
        => Array.Empty<GetExternalGameHostsResponseHost>().AsQueryable();

    public Task<IEnumerable<ExternalGameClientTeamConfig>> StartGame(ExternalGameStartMetaData metaData, CancellationToken cancellationToken)
    {
        return Task.FromResult(Array.Empty<ExternalGameClientTeamConfig>().AsEnumerable());
    }
}
