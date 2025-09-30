// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;

namespace Gameboard.Api.Data;

public enum SystemNotificationType
{
    GeneralInfo = 0,
    Warning = 1,
    Emergency = 2
}

public class SystemNotification : IEntity
{
    public string Id { get; set; }
    public string Title { get; set; }
    public string MarkdownContent { get; set; }
    public DateTimeOffset? StartsOn { get; set; }
    public DateTimeOffset? EndsOn { get; set; }
    public SystemNotificationType NotificationType { get; set; } = SystemNotificationType.GeneralInfo;
    public bool IsDeleted { get; set; }
    public bool IsDismissible { get; set; } = true;

    // nav properties
    public string CreatedByUserId { get; set; }
    public User CreatedByUser { get; set; }

    public ICollection<SystemNotificationInteraction> Interactions { get; set; } = new List<SystemNotificationInteraction>();
}
