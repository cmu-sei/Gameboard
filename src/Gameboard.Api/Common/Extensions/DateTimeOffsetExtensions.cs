using System;

namespace Gameboard.Api.Common;

public static class DateTimeOffsetExtensions
{
    public static bool IsNotEmpty(this DateTimeOffset ts)
    {
        return ts.Year > 1;
    }

    public static bool IsEmpty(this DateTimeOffset ts)
    {
        return ts.Year == 1;
    }
}
