using System.Threading.Tasks;
using Gameboard.Api.Data.Abstractions;

namespace Gameboard.Api.Features.UnityGames;

public interface IUnityStore : IStore<Data.ChallengeSpec>
{
    Task<Data.ChallengeEvent> AddUnityChallengeEvent(Data.ChallengeEvent challengeEvent);
}