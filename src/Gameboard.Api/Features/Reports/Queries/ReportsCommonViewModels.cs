namespace Gameboard.Api.Features.Reports;

public sealed class ReportSponsorViewModel
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required string LogoFileName { get; set; }
}

public sealed class ReportGameViewModel
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required bool IsTeamGame { get; set; }
    public required string Series { get; set; }
    public required string Season { get; set; }
    public required string Track { get; set; }
}
