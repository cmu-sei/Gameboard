// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using Gameboard.Api.Common.Services;

namespace Gameboard.Api.Common;

public class TimeWindow
{
    public double Now { get; }
    public double? Start { get; }
    public double? End { get; }
    public double? DurationMs { get; private set; }
    public TimeWindowState State { get; private set; }
    public double? MsTilStart { get; private set; }
    public double? MsTilEnd { get; private set; }

    public TimeWindow(DateTimeOffset now, DateTimeOffset? start, DateTimeOffset? end)
    {
        Now = now.ToUnixTimeMilliseconds();
        Start = start?.ToUnixTimeMilliseconds();
        End = end?.ToUnixTimeMilliseconds();

        State = TimeWindowState.Before;
        if (start != null && now >= start && (end is null || now < end))
        {
            State = TimeWindowState.During;
        }
        else if (end != null && now >= end)
        {
            State = TimeWindowState.After;
        }

        MsTilStart = start is null ? null : (Start - Now);
        MsTilEnd = end is null ? null : (End - Now);
        DurationMs = start == null ? null : ((End ?? Now) - Start);
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
    TimeWindow CreateWindow(DateTimeOffset now, DateTimeOffset start, DateTimeOffset end);
}

public class TimeWindowService : ITimeWindowService
{
    private readonly INowService _now;

    public TimeWindowService(INowService now)
    {
        _now = now;
    }

    public TimeWindow CreateWindow(DateTimeOffset start, DateTimeOffset end)
        => CreateWindow(_now.Get(), start, end);

    public TimeWindow CreateWindow(DateTimeOffset now, DateTimeOffset start, DateTimeOffset end)
    {
        DateTimeOffset? finalStart = start == DateTimeOffset.MinValue ? null : start;
        DateTimeOffset? finalEnd = end == DateTimeOffset.MinValue ? null : end;

        if (finalStart != null && finalEnd != null && finalStart.Value >= finalEnd.Value)
            throw new ArgumentException("Can't create a time window with end date occurring before the start date.");

        return new TimeWindow(now, finalStart, finalEnd);
    }
}
