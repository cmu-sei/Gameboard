// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using System.Linq;

namespace Gameboard.Api;

public class Ticket
{
    public string Id { get; set; }
    public int Key { get; set; }
    public string FullKey { get; set; }
    public string RequesterId { get; set; }
    public string AssigneeId { get; set; }
    public string CreatorId { get; set; }
    public string ChallengeId { get; set; }
    public bool IsTeamGame { get; set; }
    public string PlayerId { get; set; }
    public string TeamId { get; set; }
    public string TeamName { get; set; }
    public string Summary { get; set; }
    public string Description { get; set; }
    public string Status { get; set; }
    public string Label { get; set; }
    public bool StaffCreated { get; set; }

    public DateTimeOffset Created { get; set; }
    public DateTimeOffset LastUpdated { get; set; }

    public string[] Attachments { get; set; }

    public TicketUser Requester { get; set; }
    public TicketUser Assignee { get; set; }
    public TicketUser Creator { get; set; }
    public ChallengeOverview Challenge { get; set; }
    public PlayerOverview Player { get; set; }

    public List<TicketActivity> Activity { get; set; } = new List<TicketActivity>();
}

public class TicketSummary
{
    public string Id { get; set; }
    public int Key { get; set; }
    public string FullKey { get; set; }
    public string RequesterId { get; set; }
    public string AssigneeId { get; set; }
    public string CreatorId { get; set; }
    public string ChallengeId { get; set; }
    public string TeamId { get; set; }
    public string Summary { get; set; }
    public string Description { get; set; }
    public string Status { get; set; }
    public string Label { get; set; }
    public bool StaffCreated { get; set; }

    public DateTimeOffset Created { get; set; }
    public DateTimeOffset LastUpdated { get; set; }

    public UserSimple Requester { get; set; }
    public UserSimple Assignee { get; set; }
    public UserSimple Creator { get; set; }
    public ChallengeSummary Challenge { get; set; }
}

public class SelfTicketSubmission
{
    public string ChallengeId { get; set; }
    public string Summary { get; set; }
    public string Description { get; set; }
}

public class TicketSubmission
{
    public string RequesterId { get; set; }
    public string AssigneeId { get; set; }
    public string ChallengeId { get; set; }
    public string PlayerId { get; set; }
    public string GameId { get; set; }
    public string Summary { get; set; }
    public string Description { get; set; }
    public string Status { get; set; }
    public string Label { get; set; }
}

public class SelfNewTicket : SelfTicketSubmission
{
    public List<IFormFile> Uploads { get; set; }
}

public class NewTicket : TicketSubmission
{
    public List<IFormFile> Uploads { get; set; }
}

public class SelfChangedTicket : SelfTicketSubmission
{
    public string Id { get; set; }
}

public class ChangedTicket : TicketSubmission
{
    public string Id { get; set; }
}

public class NewTicketComment
{
    public string TicketId { get; set; }
    public string Message { get; set; }
    public List<IFormFile> Uploads { get; set; }
}

public class TicketActivity
{
    public string Id { get; set; }
    public string TicketId { get; set; }
    public string RequesterId { get; set; }
    public string Message { get; set; }
    public string Status { get; set; }
    public string AssigneeId { get; set; }
    public int Key { get; set; }
    public int Type { get; set; }
    public DateTimeOffset Timestamp { get; set; }
    public DateTimeOffset LastUpdated { get; set; }
    public string[] Attachments { get; set; }
    public TicketUser User { get; set; }
    public TicketUser Assignee { get; set; }
}

public class TicketSearchFilter : SearchFilter
{
    public const string OpenFilter = "open";
    public const string InProgressFilter = "in progress";
    public const string ClosedFilter = "closed";
    public const string NotClosedFilter = "not closed";
    public const string AssignedToMeFilter = "assigned to me";
    public const string UnassignedFilter = "unassigned";

    public bool WantsOpen => Filter.Contains(OpenFilter);
    public bool WantsInProgress => Filter.Contains(InProgressFilter);
    public bool WantsClosed => Filter.Contains(ClosedFilter);
    public bool WantsNotClosed => Filter.Contains(NotClosedFilter);
    public bool WantsAssignedToMe => Filter.Contains(AssignedToMeFilter);
    public bool WantsUnassigned => Filter.Contains(UnassignedFilter);

    // sane filters
    public string GameId { get; set; }
    public string WithAllLabels { get; set; }

    // Ordering logic - set up string constants, then check if they're in the request
    public const string KeyOrderString = "key";
    public const string SummaryOrderString = "summary";
    public const string StatusOrderString = "status";
    public const string CreatedOrderString = "created";
    public const string UpdatedOrderString = "updated";

    // Ordering by column
    public bool WantsOrderingByKey => OrderItem.Equals(KeyOrderString);
    public bool WantsOrderingBySummary => OrderItem.Equals(SummaryOrderString);
    public bool WantsOrderingByStatus => OrderItem.Equals(StatusOrderString);
    public bool WantsOrderingByCreated => OrderItem.Equals(CreatedOrderString);
    public bool WantsOrderingByUpdated => OrderItem.Equals(UpdatedOrderString);

    // Ordering method - descending or ascending
    public bool WantsOrderingDesc => IsDescending.Equals(true);
    public bool WantsOrderingByAsc => IsDescending.Equals(false);
}

public class TicketReportFilter : TicketSearchFilter
{
    public bool WantsGame => !GameId.IsEmpty();
    public DateTimeOffset StartRange { get; set; }
    public DateTimeOffset EndRange { get; set; }

    public bool WantsAfterStartTime => StartRange != DateTimeOffset.MinValue;
    public bool WantsBeforeEndTime => EndRange != DateTimeOffset.MinValue;
}

public class TicketDayReport
{
    public string[][] Shifts { get; set; }
    public string Timezone { get; set; }
    public TicketDayGroup[] TicketDays { get; set; }
}

public class TicketDayGroup
{
    public string Date { get; set; }
    public string DayOfWeek { get; set; }
    public int Count { get; set; }
    public int[] ShiftCounts { get; set; }
    public int OutsideShiftCount { get; set; }
}

public class TicketLabelGroup
{
    public string Label { get; set; }
    public int Count { get; set; }
}

public class TicketChallengeGroup
{
    public string ChallengeSpecId { get; set; }
    public string ChallengeTag { get; set; }
    public string ChallengeName { get; set; }
    public int Count { get; set; }
}

public class TicketNotification
{
    public string Id { get; set; }
    public int Key { get; set; }
    public string TeamId { get; set; }
    public string RequesterId { get; set; }
    public string Status { get; set; }
    public DateTimeOffset LastUpdated { get; set; }
}

public sealed class TicketUser
{
    public required string Id { get; set; }
    public required string ApprovedName { get; set; }
    public required bool IsSupportPersonnel { get; set; }
}

public sealed class TicketAttachedUser
{
    public required string TicketId { get; set; }
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required bool IsSupportPersonnel { get; set; }
    public required bool IsAssignedTo { get; set; }
    public required bool IsCreatedBy { get; set; }
    public required bool IsRequestedBy { get; set; }
    public required bool IsTeammate { get; set; }
}
