using System.Collections.Generic;

namespace Gameboard.Api.Features.Practice;

internal class UserLevelPracticeGamespaceLimitReached : GameboardException
{
    public UserLevelPracticeGamespaceLimitReached(string userId, string gameId, IEnumerable<string> teamIds)
        : base($"""User "{userId}" is already at the maximum number of gamespaces allowed for practice mode (teamIds: {string.Join(",", teamIds)}) """) { }
}
