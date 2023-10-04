namespace Gameboard.Api.Features.Challenges;

internal class NoSession : GameboardException
{
    public NoSession(string playerId) : base($"Player {playerId} does not have an active session, so it's not possible to create a challenge for them.") { }
}

internal class GamespaceLimitReached : GameboardException
{
    public GamespaceLimitReached(string gameId, string teamId) : base($""" Team(s) {teamId} are already at the maximum number of gamespaces permitted for game "{gameId}." """) { }
}
