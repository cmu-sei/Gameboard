using System;
using System.Collections.Generic;
using Gameboard.Api.Features.GameEngine;

namespace Gameboard.Api.Features.Games;

public enum GamePlayState
{
    NotStarted,
    DeployingResources,
    Starting,
    Started,
    GameOver
}

public class GameStartRequest
{
    public required IEnumerable<string> TeamIds { get; set; }
}

public sealed class GameModeStartRequest
{
    public required GameStartContext Context { get; set; }
    public required SimpleEntity Game { get; set; }
    public required PlayerCalculatedSessionWindow SessionWindow { get; set; }
}

public sealed class GameStartContext
{
    public required SimpleEntity Game { get; set; }
    public List<GameStartContextPlayer> Players { get; } = new List<GameStartContextPlayer>();
    public List<GameStartContextTeam> Teams { get; } = new List<GameStartContextTeam>();
    public List<GameStartContextChallenge> ChallengesCreated { get; } = new List<GameStartContextChallenge>();
    public required int TotalChallengeCount { get; set; }
    public List<GameEngineGameState> GamespacesStarted { get; } = new List<GameEngineGameState>();
    public List<string> GamespaceIdsStartFailed { get; } = new List<string>();
    public required int TotalGamespaceCount { get; set; }
    public required IEnumerable<string> SpecIds { get; set; }
    public DateTimeOffset StartTime { get; set; }
    public string Error { get; set; }
}

public sealed class GameStartDeployedResources
{
    public required SimpleEntity Game { get; set; }
    public IDictionary<string, GameStartDeployedTeamResources> DeployedResources { get; set; }
}

public sealed class GameStartDeployedTeamResources
{
    public required IEnumerable<GameStartDeployedChallenge> Challenges { get; set; }
}

public sealed class GameStartContextChallenge
{
    public required SimpleEntity Challenge { get; set; }
    public required GameEngineType GameEngineType { get; set; }
    public required bool IsFullySolved { get; set; }
    public required GameEngineGameState State { get; set; }
    public required string TeamId { get; set; }
}

public class GameStartContextPlayer
{
    public required SimpleEntity Player { get; set; }
    public required string TeamId { get; set; }
}

public class GameStartContextTeam
{
    public required SimpleEntity Team { get; set; }
    public required GameStartContextTeamCaptain Captain { get; set; }
    public required string HeadlessUrl { get; set; }
}

public class GameStartContextTeamCaptain
{
    public required SimpleEntity Player { get; set; }
    public required string UserId { get; set; }
}

public class GameStartDeployedChallenge
{
    public required SimpleEntity Challenge { get; set; }
    public required GameEngineType GameEngineType { get; set; }
    public required GameEngineGameState State { get; set; }
    public required string TeamId { get; set; }
}

public sealed class GameStartUpdate
{
    public required SimpleEntity Game { get; set; }
    public int ChallengesCreated { get; set; } = 0;
    public int ChallengesTotal { get; set; } = 0;
    public int GamespacesStarted { get; set; } = 0;
    public int GamespacesStartFailed { get; set; } = 0;
    public int GamespacesTotal { get; set; } = 0;
    public DateTimeOffset StartTime { get; set; }
    public required DateTimeOffset Now { get; set; }
    public string Error { get; set; }
}

public static class GameStartContextExtensions
{
    public static GameStartUpdate ToUpdate(this GameStartContext context)
    {
        return new GameStartUpdate
        {
            Game = context.Game,
            ChallengesCreated = context.ChallengesCreated.Count,
            ChallengesTotal = context.TotalChallengeCount,
            GamespacesStarted = context.GamespacesStarted.Count,
            GamespacesStartFailed = context.GamespaceIdsStartFailed.Count,
            GamespacesTotal = context.TotalGamespaceCount,
            StartTime = context.StartTime,
            Now = DateTimeOffset.UtcNow,
            Error = context.Error
        };
    }
}
