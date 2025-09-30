// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;

namespace Gameboard.Api.Data;

public class SystemNotificationInteraction : IEntity
{
    public string Id { get; set; }
    public DateTimeOffset? SawCalloutOn { get; set; }
    public DateTimeOffset? SawFullNotificationOn { get; set; }
    public DateTimeOffset? DismissedOn { get; set; }

    // navigation properties
    public string SystemNotificationId { get; set; }
    public SystemNotification SystemNotification { get; set; }

    public string UserId { get; set; }
    public User User { get; set; }
}
