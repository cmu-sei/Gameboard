using Gameboard.Api.Structure;

namespace Gameboard.Api.Features.GameEngine;

internal class TeamDoesntHaveChallenge : GameboardValidationException
{
    public TeamDoesntHaveChallenge(string teamId) : base($"Team {teamId} doesn't have any challenge data. This is probably because they haven't deployed a challenge yet.") { }
}
