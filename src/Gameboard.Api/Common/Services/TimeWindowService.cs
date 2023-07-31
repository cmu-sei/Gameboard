using System;
using Gameboard.Api.Services;

namespace Gameboard.Api.Features.Player;

public class TimeWindow
{
    public DateTimeOffset Now { get; }
    public DateTimeOffset? Start { get; }
    public DateTimeOffset? End { get; }
    public double? DurationMs { get; private set; }
    public TimeWindowState State { get; private set; }
    public double? MsTilStart { get; private set; }
    public double? MsTilEnd { get; private set; }

    public TimeWindow(DateTimeOffset now, DateTimeOffset? start, DateTimeOffset? end)
    {
        Now = now;
        Start = start;
        End = end;

        State = TimeWindowState.Before;
        if (start != null && now >= start && (end is null || now < end))
        {
            State = TimeWindowState.During;
        }
        else if (end != null && now >= end)
        {
            State = TimeWindowState.After;
        }

        MsTilStart = start is null ? null : (start - now).Value.TotalMilliseconds;
        MsTilEnd = end is null ? null : (end - now).Value.TotalMilliseconds;
        DurationMs = start == null ? null : ((end ?? now) - start).Value.TotalMilliseconds;
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
        DateTimeOffset? finalStart = start == DateTimeOffset.MinValue ? null : start;
        DateTimeOffset? finalEnd = end == DateTimeOffset.MinValue ? null : end;

        if (finalStart != null && finalEnd != null && finalStart.Value >= finalEnd.Value)
            throw new ArgumentException("Can't create a time window with end date occurring before the start date.");

        return new TimeWindow(_now.Get(), finalStart, finalEnd);
    }
}
