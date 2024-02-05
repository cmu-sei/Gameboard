// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;

namespace Gameboard.Api.Data;

public class User : IEntity
{
    public string Id { get; set; }
    public string Username { get; set; }
    public string Email { get; set; }
    public string Name { get; set; }
    public string NameStatus { get; set; }
    public string ApprovedName { get; set; }
    public UserRole Role { get; set; }
    public DateTimeOffset CreatedOn { get; set; }
    public DateTimeOffset? LastLoginDate { get; set; }
    public int LoginCount { get; set; }
    public bool HasDefaultSponsor { get; set; }
    public bool PlayAudioOnBrowserNotification { get; set; }

    // navigation properties
    public string SponsorId { get; set; }
    public Sponsor Sponsor { get; set; }
    public ICollection<ApiKey> ApiKeys { get; set; } = new List<ApiKey>();
    public ICollection<SystemNotification> CreatedSystemNotifications { get; set; } = new List<SystemNotification>();
    public ICollection<Player> Enrollments { get; set; } = new List<Player>();
    public ICollection<ManualBonus> EnteredManualBonuses { get; set; } = new List<ManualBonus>();
    public ICollection<Feedback> Feedback { get; set; } = new List<Feedback>();
    public ICollection<PublishedCompetitiveCertificate> PublishedCompetitiveCertificates { get; set; } = new List<PublishedCompetitiveCertificate>();
    public ICollection<PublishedPracticeCertificate> PublishedPracticeCertificates { get; set; } = new List<PublishedPracticeCertificate>();
    public ICollection<SystemNotificationInteraction> SystemNotificationInteractions { get; set; } = new List<SystemNotificationInteraction>();
    public PracticeModeSettings UpdatedPracticeModeSettings { get; set; }
    public SupportSettings UpdatedSupportSettings { get; set; }
}
