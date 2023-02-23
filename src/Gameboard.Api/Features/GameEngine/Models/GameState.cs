using System;
using System.Collections.Generic;

namespace Gameboard.Api.Features.GameEngine;

public class GameState : IGameState
{
    public string Id { get; }
    public string Name { get; }
    public string ManagerId { get; }
    public string ManagerName { get; }
    public string Markdown { get; }
    public string Audience { get; }
    public string LaunchpointUrl { get; }
    public bool IsActive { get; }

    public IEnumerable<IGameStatePlayer> Players { get; }
    public DateTimeOffset WhenCreated { get; }
    public DateTimeOffset StartTime { get; }
    public DateTimeOffset EndTime { get; }
    public DateTimeOffset ExpirationTime { get; }
    public IEnumerable<IGameStateVmState> Vms { get; }
    public IGameStateChallengeView Challenge { get; }
}
