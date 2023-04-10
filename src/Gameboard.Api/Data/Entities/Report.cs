using System.Collections.Generic;
using Gameboard.Api.Data;

public class Report : IEntity
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required string Description { get; set; }
    public required ICollection<ReportParameter> Parameters { get; set; }
}

public class ReportParameter
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public string Description { get; set; }
    public required ReportParameterType ParameterType { get; set; }
}

public enum ReportParameterType
{
    Challenge,
    CompetitionTrack,
    DateRange,
    DateSingle,
    Game,
    Player,
    Sponsor,
    Team
}
