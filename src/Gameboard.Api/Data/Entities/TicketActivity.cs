// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gameboard.Api.Data
{
    public class TicketActivity  : IEntity
    {
        public string Id { get; set; }
        public string TicketId { get; set; }
        public string UserId { get; set; }
        public string AssigneeId { get; set; }
        public string Message { get; set; }
        public string Status { get; set; }
        public ActivityType Type { get; set; }
        public DateTimeOffset Timestamp { get; set; }
        public string Attachments { get; set; }
        public Ticket Ticket { get; set; }
        public User User { get; set; }
        [ForeignKey("AssigneeId")]
        public User Assignee { get; set; }
    }

}
