using System;

namespace Gameboard.Api.Common;

public sealed class DateRange
{
    public DateTimeOffset Start { get; set; }
    public DateTimeOffset End { get; set; }
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

public class SimpleEntity
{
    public string Id { get; set; }
    public string Name { get; set; }
}
