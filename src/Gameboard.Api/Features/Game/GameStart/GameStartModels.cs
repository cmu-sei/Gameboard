using System.Collections.Generic;
using Gameboard.Api.Common;
using Gameboard.Api.Features.GameEngine;

namespace Gameboard.Api.Features.Games;

public class GameStartContext
{
    public required SimpleEntity Game { get; set; }
    public required IList<Challenge> DeployedChallenges { get; set; }
    public required IList<GameEngineGameState> DeployedGamespaces { get; set; }
    public required IEnumerable<SimpleEntity> Teams { get; set; }
}

public class GameStartRequest
{
    public required string GameId { get; set; }
    public User ActingUser { get; set; }
}

public class SyncGameStartRequest
{
    public required User ActingUser { get; set; }
    public required string GameId { get; set; }
}
