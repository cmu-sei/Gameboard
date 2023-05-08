using System;
using System.Collections.Generic;
using Gameboard.Api.Structure;

namespace Gameboard.Api.Features.Reports;

public enum SupportReportTicketWindow
{
    BusinessHours,
    EveningHours,
    OffHours
}

public enum SupportReportLabelsModifier
{
    HasAll,
    HasAny
}

public class SupportReportParameters
{
    public string ChallengeSpecId { get; set; }
    public string GameId { get; set; }
    public IEnumerable<string> Labels { get; set; }
    public Nullable<SupportReportLabelsModifier> LabelsModifier { get; set; }
    public Nullable<double> HoursSinceOpen { get; set; }
    public Nullable<double> HoursSinceStatusChange { get; set; }
    public DateTimeOffset? OpenedDateStart { get; set; }
    public DateTimeOffset? OpenedDateEnd { get; set; }
    public Nullable<SupportReportTicketWindow> OpenedWindow { get; set; }
    public string Status { get; set; }
}

public class SupportReportRecord
{
    public required int Key { get; set; }
    public required string PrefixedKey { get; set; }
    public required DateTimeOffset CreatedOn { get; set; }
    public required DateTimeOffset UpdatedOn { get; set; }
    public required SimpleEntity AssignedTo { get; set; }
    public required SimpleEntity CreatedBy { get; set; }
    public required SimpleEntity UpdatedBy { get; set; }
    public required SimpleEntity RequestedBy { get; set; }
    public required SimpleEntity Game { get; set; }
    public required SimpleEntity Challenge { get; set; }
    public required IEnumerable<string> AttachmentUris { get; set; }
    public required IEnumerable<string> Labels { get; set; }
    public required string Summary { get; set; }
    public required string Status { get; set; }
    public required int ActivityCount { get; set; }
}
