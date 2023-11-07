using System;
using System.Collections.Generic;
using Gameboard.Api.Features.GameEngine;

namespace Gameboard.Api.Features.Games;

public enum GameStartPhase
{
    NotStarted,
    Starting,
    Started,
    GameOver,
    Failed
}

public class GameStartRequest
{
    public required string GameId { get; set; }
    public required bool IsPreDeployRequest { get; set; }
}

public sealed class GameModeStartRequest
{
    public required SimpleEntity Game { get; set; }
    public required GameStartContext Context { get; set; }
    public required bool IsPreDeployRequest { get; set; }
}

public sealed class GameStartContext
{
    public required SimpleEntity Game { get; set; }
    public List<GameStartContextPlayer> Players { get; } = new List<GameStartContextPlayer>();
    public List<GameStartContextTeam> Teams { get; } = new List<GameStartContextTeam>();
    public List<GameStartDeployedChallenge> ChallengesCreated { get; } = new List<GameStartDeployedChallenge>();
    public required int TotalChallengeCount { get; set; }
    public List<GameEngineGameState> GamespacesStarted { get; } = new List<GameEngineGameState>();
    public required int TotalGamespaceCount { get; set; }
    public required double SessionLengthMinutes { get; set; }
    public required IEnumerable<string> SpecIds { get; set; }
    public DateTimeOffset StartTime { get; set; }
    public string Error { get; set; }
}

public sealed class GameStartDeployedResources
{
    public required SimpleEntity Game { get; set; }
    public IDictionary<string, IList<GameStartDeployedTeamResources>> DeployedResources { get; set; }
}

public sealed class GameStartDeployedTeamResources
{
    public required string TeamId { get; set; }
    public required IEnumerable<GameStartDeployedChallenge> Resources { get; set; }
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
    public required GameEngineGameState Gamespace { get; set; }
    public required GameEngineType GameEngineType { get; set; }
    public required string TeamId { get; set; }
}

public sealed class GameStartUpdate
{
    public required SimpleEntity Game { get; set; }
    public int ChallengesCreated { get; set; } = 0;
    public int ChallengesTotal { get; set; } = 0;
    public int GamespacesStarted { get; set; } = 0;
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
            GamespacesTotal = context.TotalGamespaceCount,
            StartTime = context.StartTime,
            Now = DateTimeOffset.UtcNow,
            Error = context.Error
        };
    }
}
