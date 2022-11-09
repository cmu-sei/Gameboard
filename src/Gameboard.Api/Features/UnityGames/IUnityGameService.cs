using System.Collections.Generic;
using System.Threading.Tasks;
using Gameboard.Api.Data;

namespace Gameboard.Api.Features.UnityGames;

public interface IUnityGameService
{
    Task<ChallengeEvent> AddChallengeEvent(NewUnityChallengeEvent model, string userId);
    Task<Data.Challenge> AddChallenge(NewUnityChallenge newChallenge, User actor);
    Task CreateMissionEvent(UnityMissionUpdate model, Api.User actor);
    Task DeleteChallengeData(string gameId);
}