using System;

namespace Gameboard.Api.Common;

public class SimpleEntity
{
    public string Id { get; set; }
    public string Name { get; set; }
}

public sealed class DateRange
{
    public DateTimeOffset Start { get; set; }
    public DateTimeOffset End { get; set; }
}
