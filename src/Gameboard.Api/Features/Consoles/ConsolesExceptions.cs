using Gameboard.Api.Structure;

namespace Gameboard.Api.Features.Consoles;

public sealed class ConsoleTeamNoAccessException(string teamId) : GameboardValidationException($"You can't access consoles owned by team {teamId}.")
{
}
