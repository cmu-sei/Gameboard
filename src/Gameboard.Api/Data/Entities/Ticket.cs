// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gameboard.Api.Data
{
    public class Ticket : IEntity
    {
        public string Id { get; set; } // PK
        public int Key { get; set; } // 
        public string RequesterId { get; set; } // FK of User.Id
        public string AssigneeId { get; set; } // FK of User.Id
        public string CreatorId { get; set; } // FK of User.Id
        public string ChallengeId { get; set; } // FK of Challenge.Id (optional based on ticket type)
        public string PlayerId { get; set; } // ____
        public string TeamId { get; set; } // Reference to Player.TeamId (optional based on ticket type and team game)
        public string Summary { get; set; } // Limited to size ___
        public string Description { get; set; } // Text limited to size ____
        public string Status { get; set; } // String or enum?
        public string Label { get; set; } // String, space delimited?
        public bool StaffCreated { get; set; }
        // public bool SelfCreated { get; set; } 
        // type?
            // could you infer this based on what FK references were set
        public DateTimeOffset Created { get; set; } // When CREATED
        public DateTimeOffset LastUpdated { get; set; } // When updated last
        public string Attachments { get; set; } // JSON paths to static files
        
        [ForeignKey("RequesterId")]
        public User Requester { get; set; }
        [ForeignKey("AssigneeId")]
        public User Assignee { get; set; }
        [ForeignKey("CreatorId")]
        public User Creator { get; set; }
        public Challenge Challenge { get; set; }
        public Player Player { get; set; }

        // thread of comments and activity like status or assignee changes
        public ICollection<TicketActivity> Activity { get; set; } = new List<TicketActivity>();
    }

}
