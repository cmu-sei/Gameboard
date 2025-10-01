// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;

namespace Gameboard.Api.Features.Teams;

public interface ISessionWindowCalculator
{
    CalculatedSessionWindow Calculate(int sessionMinutes, DateTimeOffset gameEnd, bool isElevatedUser, DateTimeOffset sessionStart);
    CalculatedSessionWindow Calculate(Data.Game game, bool isElevatedUser, DateTimeOffset sessionStart);
}

internal class SessionWindowCalculator : ISessionWindowCalculator
{
    public CalculatedSessionWindow Calculate(Data.Game game, bool isElevatedUser, DateTimeOffset sessionStart)
        => Calculate(game.SessionMinutes, game.GameEnd, isElevatedUser, sessionStart);

    public CalculatedSessionWindow Calculate(int sessionMinutes, DateTimeOffset gameEnd, bool isElevatedUser, DateTimeOffset sessionStart)
    {
        var normalSessionEnd = sessionStart.AddMinutes(sessionMinutes);
        var finalSessionEnd = normalSessionEnd;

        if (!isElevatedUser && gameEnd < normalSessionEnd)
            finalSessionEnd = gameEnd;

        return new()
        {
            Start = sessionStart,
            End = finalSessionEnd,
            LengthInMinutes = (finalSessionEnd - sessionStart).TotalMinutes,
            IsLateStart = finalSessionEnd < normalSessionEnd
        };
    }
}
