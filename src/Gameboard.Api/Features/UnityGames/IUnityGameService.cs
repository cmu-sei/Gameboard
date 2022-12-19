using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Gameboard.Api.Features.UnityGames;

public interface IUnityGameService
{
    Task<Data.Challenge> AddChallenge(NewUnityChallenge newChallenge, User actor);
    Task<Data.ChallengeEvent> CreateMissionEvent(UnityMissionUpdate model, Api.User actor);
    Task<Data.Challenge> HasChallengeData(string gamespaceId);
    Task DeleteChallengeData(string gameId);
    bool IsUnityGame(Game game);
    bool IsUnityGame(Data.Game game);
    Regex GetMissionCompleteEventRegex();
    string GetUnityModeString();
}
