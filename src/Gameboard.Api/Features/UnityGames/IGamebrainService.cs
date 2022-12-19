using System.Collections.Generic;
using System.Threading.Tasks;

namespace Gameboard.Api.Features.UnityGames;

public interface IGamebrainService
{
    Task<string> DeployUnitySpace(string gameId, string teamId);
    Task<string> GetGameState(string gameId, string teamId);
    Task<string> UndeployUnitySpace(string gameId, string teamId);
    Task UpdateConsoleUrls(string gameId, string teamId, IEnumerable<UnityGameVm> vms);
}
