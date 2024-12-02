using System;
using Gameboard.Api.Structure;

namespace Gameboard.Api.Features.Teams;

public sealed class AddToTeamResponse
{
    public required SimpleEntity Game { get; set; }
    public required SimpleEntity Player { get; set; }
    public required SimpleEntity Team { get; set; }
    public required SimpleEntity User { get; set; }
}

internal sealed class UserAlreadyPlayed : GameboardValidationException
{
    public UserAlreadyPlayed(SimpleEntity user, SimpleEntity game, string teamId, DateTimeOffset sessionStart)
        : base($"""User "{user.Name}" already played game {game.Name} on {sessionStart} (team {teamId})""") { }
}
