using System;

namespace Gameboard.Api.Features.Player;

public interface ISessionWindowCalculator
{
    PlayerCalculatedSessionWindow CalculateSessionWindow(Data.Game game, bool isElevatedUser, DateTimeOffset sessionStart);
}

internal class SessionWindowCalculator : ISessionWindowCalculator
{
    public PlayerCalculatedSessionWindow CalculateSessionWindow(Data.Game game, bool isElevatedUser, DateTimeOffset sessionStart)
    {
        var normalSessionEnd = sessionStart.AddMinutes(game.SessionMinutes);
        var finalSessionEnd = normalSessionEnd;

        if (!isElevatedUser && game.GameEnd < normalSessionEnd)
            finalSessionEnd = game.GameEnd;

        return new()
        {
            Start = sessionStart,
            End = finalSessionEnd,
            LengthInMinutes = (finalSessionEnd - sessionStart).TotalMinutes,
            IsLateStart = finalSessionEnd < normalSessionEnd
        };
    }
}
