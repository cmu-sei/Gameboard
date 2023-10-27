using System;
using System.Collections.Generic;
using Gameboard.Api.Features.GameEngine;

namespace Gameboard.Api.Features.Games;

public class GameStartState
{
    public required SimpleEntity Game { get; set; }
    public List<GameStartStateChallenge> ChallengesCreated { get; private set; } = new List<GameStartStateChallenge>();
    public int ChallengesTotal { get; set; } = 0;
    public List<GameEngineGameState> GamespacesStarted { get; set; } = new List<GameEngineGameState>();
    public int GamespacesTotal { get; set; } = 0;
    public List<GameStartStatePlayer> Players { get; } = new List<GameStartStatePlayer>();
    public List<GameStartStateTeam> Teams { get; } = new List<GameStartStateTeam>();
    public DateTimeOffset StartTime { get; set; }
    public required DateTimeOffset Now { get; set; }
    public string Error { get; set; }

    public double OverallProgress
    {
        get
        {
            if (GamespacesTotal == 0 || ChallengesTotal == 0)
                return 0;

            var retVal = Math.Round(((0.8 * GamespacesStarted.Count) / GamespacesTotal) + ((0.2 * ChallengesCreated.Count) / ChallengesTotal));
            return double.IsRealNumber(retVal) ? retVal : 0;
        }
    }
}

public enum GameStartPhase
{
    NotStarted,
    Starting,
    Started,
    GameOver
}

public class GameStartStatePlayer
{
    public required SimpleEntity Player { get; set; }
    public required string TeamId { get; set; }
}

public class GameStartStateTeam
{
    public required SimpleEntity Team { get; set; }
    public required GameStartStateTeamCaptain Captain { get; set; }
    public required string HeadlessUrl { get; set; }
}

public class GameStartStateTeamCaptain
{
    public required SimpleEntity Player { get; set; }
    public required string UserId { get; set; }
}

public class GameStartStateChallenge
{
    public required SimpleEntity Challenge { get; set; }
    public required GameEngineType GameEngineType { get; set; }
    public required string TeamId { get; set; }
}

public class GameStartRequest
{
    public required string GameId { get; set; }
}

public class GameModeStartRequest
{
    public required string GameId { get; set; }
    public required GameStartState State { get; set; }
    public required GameModeStartRequestContext Context { get; set; }
}

// contains metadata assembled by the game start service that per-mode child services
// will use but don't want to send to the client
public class GameModeStartRequestContext
{
    public required double SessionLengthMinutes { get; set; }
    public required IEnumerable<string> SpecIds { get; set; }
}
