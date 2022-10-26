using Gameboard.Api.Data;
using Gameboard.Api.Data.Abstractions;

namespace Gameboard.Api.Features.ChallengeEvents;

public interface IChallengeEventStore : IStore<ChallengeEvent>
{
    // IStore enforces everything we're using right now
}