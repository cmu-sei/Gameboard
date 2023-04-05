using System.Collections.Generic;
using System.Linq;
using Gameboard.Api.Structure;

namespace Gameboard.Api.Features.Games;

internal class CantSynchronizeNonSynchronizedGame : GameboardValidationException
{
    public CantSynchronizeNonSynchronizedGame(string gameId) : base($"Can't enforce synchronized start on non-synchronized game \"{gameId}\".") { }
}

internal class CantStartNonReadySynchronizedGame : GameboardValidationException
{
    public CantStartNonReadySynchronizedGame(string gameId, IEnumerable<SyncStartPlayer> nonReadyPlayers) : base($"Can't start synchronized game \"{gameId}\" - {nonReadyPlayers.Count()} players aren't ready (\"{string.Join(",", nonReadyPlayers.Select(p => p.Id))}\").") { }
}

internal class PracticeSessionLimitReached : GameboardValidationException
{
    public PracticeSessionLimitReached(string userId, int userSessionCount, int practiceSessionLimit) : base($"Can't start a new practice session. User \"{userId}\" has \"{userSessionCount}\" practice sessions, and the limit is \"{practiceSessionLimit}\".") { }
}

internal class SessionLimitReached : GameboardValidationException
{
    public SessionLimitReached(string teamId, string gameId, int sessions, int sessionLimit) : base($"Can't start a new game ({gameId}) for team \"{teamId}\". The session limit is {sessionLimit}, and the team has {sessions} sessions.") { }
}
