namespace Gameboard.Api.Features.Consoles;

public enum ListConsolesRequestSort
{
    Rank,
    TeamName
}

public sealed class ListConsolesResponse
{
    public ListConsolesResponseConsole[] Consoles { get; set; }
}

public sealed class ListConsolesResponseConsole
{
    // these are duplicated properties for now, but it's easier to group on the client side with them
    public required string TeamId { get; set; }

    public required ConsoleId ConsoleId { get; set; }
    public required string AccessTicket { get; set; }
    public required SimpleEntity[] ActiveUsers { get; set; }
    public required ListConsolesResponseChallenge Challenge { get; set; }
    public required bool IsViewOnly { get; set; }
    public required ListConsolesResponseTeam Team { get; set; }
    public required string Url { get; set; }
}

public sealed class ListConsolesResponseChallenge
{
    public required string Id { get; set; }
    public required bool IsPractice { get; set; }
    public required string Name { get; set; }
    public required string SpecId { get; set; }
}

public sealed class ListConsolesResponseTeam
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required int? Rank { get; set; }
    public required double? Score { get; set; }
}
