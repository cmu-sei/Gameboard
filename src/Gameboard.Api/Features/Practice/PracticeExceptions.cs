using System.Collections.Generic;
using System.Linq;
using Gameboard.Api.Structure;

namespace Gameboard.Api.Features.Practice;

internal class CantExtendPracticeSessionNoneActive : GameboardValidationException
{
    public CantExtendPracticeSessionNoneActive(string userId)
        : base($"""Can't extend practice session for user "{userId}": They don't have a practice session active.""") { }
}

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

internal class ChallengeGroupInvalidParentException : GameboardValidationException
{
    public ChallengeGroupInvalidParentException(string invalidParentGroupId, string itsParentGroupId)
        : base($"""Parent group "{invalidParentGroupId}" is an invalid parent because it has its own parent ("{itsParentGroupId}"). We only allow one level of nesting of challenge groups.""") { }
}

internal class ChallengeGroupInvalidUpdateBecauseParentsAndKidsException : GameboardValidationException
{
    public ChallengeGroupInvalidUpdateBecauseParentsAndKidsException(string groupId, string parentGroupId, int childCount)
        : base($"""Group "{groupId}" can't be updated to have parent "{parentGroupId}" because it has {childCount} child groups. Groups with child groups can't also have a parent group (no multiple nesting).""") { }
}

internal class UserLevelPracticeGamespaceLimitReached : GameboardValidationException
{
    public UserLevelPracticeGamespaceLimitReached(string userId, string gameId, IEnumerable<string> teamIds)
        : base($"""User "{userId}" is already at the maximum number of gamespaces allowed for practice mode (teamIds: {string.Join(",", teamIds)}) """) { }
}
