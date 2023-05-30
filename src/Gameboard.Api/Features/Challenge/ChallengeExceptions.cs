namespace Gameboard.Api.Features.Challenges;

internal class NoSession : GameboardException
{
    public NoSession(string playerId) : base($"Player {playerId} does not have an active session, so it's not possible to create a challenge for them.") { }
}
