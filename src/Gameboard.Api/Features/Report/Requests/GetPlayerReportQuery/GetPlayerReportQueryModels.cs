using System.Collections.Generic;

namespace Gameboard.Api.Features.Reports;

public class PlayerReportQueryParameters
{
    public string ChallengeId { get; set; }
    public string GameId { get; set; }
    public ReportDateRange SessionStartWindow { get; set; }
}

public class PlayerReportRecord
{

}

public sealed class PlayerReportQueryResults : IReportResult<PlayerReportRecord>
{
    public required ReportMetaData MetaData { get; set; }
    public required IEnumerable<PlayerReportRecord> Records { get; set; }
}
