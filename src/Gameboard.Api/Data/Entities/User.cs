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
    public string Sponsor { get; set; }
    public UserRole Role { get; set; }
    public DateTimeOffset CreatedOn { get; set; }
    public DateTimeOffset? LastLoginDate { get; set; }
    public int LoginCount { get; set; }

    // relational properties
    public ICollection<ApiKey> ApiKeys { get; set; } = new List<ApiKey>();
    public ICollection<Player> Enrollments { get; set; } = new List<Player>();
    public ICollection<ManualChallengeBonus> EnteredManualChallengeBonuses { get; set; } = new List<ManualChallengeBonus>();
    public ICollection<Feedback> Feedback { get; set; } = new List<Feedback>();
    public ICollection<PublishedCompetitiveCertificate> PublishedCompetitiveCertificates { get; set; } = new List<PublishedCompetitiveCertificate>();
    public ICollection<PublishedPracticeCertificate> PublishedPracticeCertificates { get; set; } = new List<PublishedPracticeCertificate>();
    public PracticeModeSettings UpdatedPracticeModeSettings { get; set; }
}
