using System;
using Gameboard.Api.Services;

namespace Gameboard.Api.Features.Player;

public class TimeWindow
{
    public DateTimeOffset Now { get; }
    public DateTimeOffset Start { get; }
    public DateTimeOffset End { get; }
    public TimeSpan Duration { get; private set; }
    public TimeWindowState State { get; private set; }
    public TimeSpan? TimeUntilStart { get; private set; }
    public TimeSpan? TimeUntilEnd { get; private set; }

    public TimeWindow(DateTimeOffset now, DateTimeOffset start, DateTimeOffset end)
    {
        Now = now;
        Start = start;
        End = end;

        State = TimeWindowState.Before;
        if (now >= start && now < end)
        {
            State = TimeWindowState.During;
        }
        else if (now >= end)
        {
            State = TimeWindowState.After;
        }

        TimeUntilStart = (now < start ? start - now : null);
        TimeUntilEnd = (now < end ? end - now : null);
        Duration = end - start;
    }
}

public enum TimeWindowState
{
    Before,
    During,
    After
}

public interface ITimeWindowService
{
    TimeWindow CreateWindow(DateTimeOffset start, DateTimeOffset end);
}

public class TimeWindowService : ITimeWindowService
{
    private readonly INowService _now;

    public TimeWindowService(INowService now)
    {
        _now = now;
    }

    public TimeWindow CreateWindow(DateTimeOffset start, DateTimeOffset end)
    {
        if (start >= end)
            throw new ArgumentException("Can't create a time window with end date occurring before the start date.");

        return new TimeWindow(_now.Get(), start, end);
    }
}
