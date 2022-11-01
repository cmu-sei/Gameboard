using System.Collections.Generic;
using System.Threading.Tasks;
using Gameboard.Api.Data.Abstractions;

namespace Gameboard.Api.Features.UnityGames;

public interface IUnityStore : IStore<Data.ChallengeSpec>
{
    Task<IEnumerable<Data.ChallengeEvent>> AddUnityChallengeEvents(IEnumerable<Data.ChallengeEvent> challengeEvents);
}