using System;
using System.Collections.Generic;
using Gameboard.Api.Common;

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
    public string Labels { get; set; }
    public double? MinutesSinceOpen { get; set; }
    public double? MinutesSinceUpdate { get; set; }
    public DateTimeOffset? OpenedDateStart { get; set; }
    public DateTimeOffset? OpenedDateEnd { get; set; }
    public SupportReportTicketWindow? OpenedWindow { get; set; }
    public string Statuses { get; set; }
}

public class SupportReportRecord
{
    public required int Key { get; set; }
    public required string PrefixedKey { get; set; }
    public required DateTimeOffset CreatedOn { get; set; }
    public required DateTimeOffset? UpdatedOn { get; set; }
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

public class SupportReportExportRecord
{
    public required string PrefixedKey { get; set; }
    public DateTimeOffset CreatedOn { get; set; }
    public DateTimeOffset UpdatedOn { get; set; }
    public string AssignedTo { get; set; }
    public string CreatedBy { get; set; }
    public string UpdatedBy { get; set; }
    public string RequestedBy { get; set; }
    public string Game { get; set; }
    public string Attachments { get; set; }
    public string Labels { get; set; }
    public string Summary { get; set; }
    public string Status { get; set; }
    public int ActivityCount { get; set; }
}
