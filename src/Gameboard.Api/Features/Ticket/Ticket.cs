// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using System.Linq;

namespace Gameboard.Api
{
    public class Ticket
    {
        public string Id { get; set; } 
        public int Key { get; set; } 
        public string FullKey { get; set; }
        public string RequesterId { get; set; }
        public string AssigneeId { get; set; }
        public string CreatorId { get; set; }
        public string ChallengeId { get; set; }
        public string PlayerId { get; set; }
        public string TeamId { get; set; }
        public string Summary { get; set; }
        public string Description { get; set; }
        public string Status { get; set; }
        public string Label { get; set; }
        public bool StaffCreated { get; set; }

        public DateTimeOffset Created { get; set; }
        public DateTimeOffset LastUpdated { get; set; }

        public string[] Attachments { get; set; }
        
        public UserSummary Requester { get; set; }
        public UserSummary Assignee { get; set; }
        public UserSummary Creator { get; set; }
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
        
        public UserSummary Requester { get; set; }
        public UserSummary Assignee { get; set; }
        public UserSummary Creator { get; set; }
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
        public string Message { get; set; }
        public string Status { get; set; }
        public string AssigneeId { get; set; }
        public int Type { get; set; }
        public DateTimeOffset Timestamp { get; set; }
        public string[] Attachments { get; set; }
        public UserSummary User { get; set; }
        public UserSummary Assignee { get; set; }
    }

    public class UploadFile 
    {
        public string FileName { get; set; }
        public IFormFile File { get; set; }
    }

    public class TicketSearchFilter: SearchFilter
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
    }

    public class TicketReportFilter: SearchFilter
    {
        public string GameId { get; set; }
        public bool WantsGame => !GameId.IsEmpty();
        public DateTimeOffset StartRange { get; set; }
        public DateTimeOffset EndRange { get; set; }

        public bool WantsAfterStartTime => StartRange != DateTimeOffset.MinValue;
        public bool WantsBeforeEndTime => EndRange != DateTimeOffset.MinValue;
    }

    public class TicketDayGroup
    {
        public string Date { get; set; }
        public string DayOfWeek { get; set; }
        public int Count { get; set; }
        public int Shift1Count { get; set; }
        public int Shift2Count { get; set; }
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
}
