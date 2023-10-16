using System.Collections.Generic;
using System.Linq;
using Gameboard.Api.Structure;

namespace Gameboard.Api.Features.Practice;

internal class CantExtendNonPracticeSession : GameboardValidationException
{
    public CantExtendNonPracticeSession(string teamId, IEnumerable<string> nonPracticePlayerIds)
        : base($"""Can't extend practice session for team "{teamId}": {nonPracticePlayerIds.Count()} players are not in practice mode.""") { }
}

internal class CantExtendEndedPracticeSession : GameboardValidationException
{
    public CantExtendEndedPracticeSession(string teamId)
        : base($"""Can't extend practice session for team "{teamId}" because it has already ended.""") { }
}

internal class UserLevelPracticeGamespaceLimitReached : GameboardValidationException
{
    public UserLevelPracticeGamespaceLimitReached(string userId, string gameId, IEnumerable<string> teamIds)
        : base($"""User "{userId}" is already at the maximum number of gamespaces allowed for practice mode (teamIds: {string.Join(",", teamIds)}) """) { }
}
