using System;

namespace Gameboard.Api.Features.Games;

public sealed class GameSessionAvailibilityResponse
{
    public int SessionsAvailable { get; set; }
    public DateTimeOffset? NextSessionEnd { get; set; }
}
