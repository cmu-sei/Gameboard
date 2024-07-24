using System.Collections.Generic;

namespace Gameboard.Api.Features.Admin;

public sealed class GetPlayersCsvExportResponse
{
    public required IEnumerable<GetPlayersCsvExportResponsePlayer> Players { get; set; }
}

public sealed class GetPlayersCsvExportResponsePlayer
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required SimpleEntity Game { get; set; }
    public required SimpleEntity Team { get; set; }
    public required SimpleEntity User { get; set; }
    public required DateRange Session { get; set; }
    public required int? Rank { get; set; }
    public required double? Score { get; set; }
    public required int SolvesCorrectCount { get; set; }
    public required int SolvesPartialCount { get; set; }
    public required long? TimeMs { get; set; }
}
