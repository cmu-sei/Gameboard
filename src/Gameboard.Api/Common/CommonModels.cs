using System;

namespace Gameboard.Api.Common;

public sealed class DateRange
{
    public DateTimeOffset Start { get; set; }
    public DateTimeOffset End { get; set; }

    public DateRange() { }

    public DateRange(DateTimeOffset start, DateTimeOffset end)
    {
        Start = start;
        End = end;
    }
}

public sealed class GameCardContext
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required string EngineMode { get; set; }
    public required int LiveSessionCount { get; set; }
    public required string Logo { get; set; }
    public required bool IsPractice { get; set; }
    public required bool IsPublished { get; set; }
    public required bool IsTeamGame { get; set; }
}

public sealed class PagingParameters
{
    public required int PageNumber { get; set; }
    public required int PageSize { get; set; }
}

public sealed class PlayerWithSponsor
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required SimpleSponsor Sponsor { get; set; }
}

public sealed class SimpleSponsor
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Logo { get; set; }
}

public sealed class SimpleEntity
{
    public string Id { get; set; }
    public string Name { get; set; }
}

public enum SortDirection
{
    Asc,
    Desc
}
