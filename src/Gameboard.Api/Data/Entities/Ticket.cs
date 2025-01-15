// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gameboard.Api.Data
{
    public class Ticket : IEntity
    {
        public string Id { get; set; } // PK
        public int Key { get; set; } // Serial
        public string RequesterId { get; set; } // FK of User.Id
        public string AssigneeId { get; set; } // FK of User.Id
        public string CreatorId { get; set; } // FK of User.Id
        public string ChallengeId { get; set; } // FK of Challenge.Id (optional based on ticket context)
        public string PlayerId { get; set; } // Fk of Player.Id (optional based on ticket context)
        public string TeamId { get; set; } // Reference to Player.TeamId (optional based on ticket context)
        public string Summary { get; set; } // Limited to size 128
        public string Description { get; set; }
        public string Status { get; set; }
        public string Label { get; set; } // String, space delimited for multiple
        public bool StaffCreated { get; set; }
        public DateTimeOffset Created { get; set; } // Time ticket was created, does not change
        public DateTimeOffset LastUpdated { get; set; } // Time ticket was last updated including when activity was added for this ticket
        public string Attachments { get; set; } // JSON array of string filenames for static files behind the support path
        [ForeignKey("RequesterId")]
        public User Requester { get; set; }
        [ForeignKey("AssigneeId")]
        public User Assignee { get; set; }
        [ForeignKey("CreatorId")]
        public User Creator { get; set; }
        public Challenge Challenge { get; set; }
        public Player Player { get; set; }

        // Activity is a thread of comments and activity like status or assignee changes
        public ICollection<TicketActivity> Activity { get; set; } = [];
    }
}
