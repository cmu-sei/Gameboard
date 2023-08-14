using Gameboard.Api.Features.UnityGames;

namespace Gameboard.Api.Tests.Integration.Fixtures;

internal class TestGamebrainService : IGamebrainService
{
    public Task<string> DeployUnitySpace(string gameId, string teamId)
    {
        return Task.FromResult("{}");
    }

    public Task<string> GetGameState(string gameId, string teamId)
    {
        return Task.FromResult("{}");
    }

    public Task<string> UndeployUnitySpace(string gameId, string teamId)
    {
        return Task.FromResult(string.Empty);
    }

    public Task UpdateConsoleUrls(string gameId, string teamId, IEnumerable<UnityGameVm> vms)
    {
        return Task.CompletedTask;
    }
}
