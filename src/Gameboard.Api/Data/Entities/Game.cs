// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gameboard.Api.Data;

public class Game : IEntity
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Competition { get; set; }
    public string Season { get; set; }
    public string Track { get; set; }
    public string Division { get; set; }
    public string Logo { get; set; }
    public string Sponsor { get; set; }
    public string Background { get; set; }
    public string TestCode { get; set; }
    public DateTimeOffset GameStart { get; set; }
    public DateTimeOffset GameEnd { get; set; }
    public string GameMarkdown { get; set; }
    public string FeedbackConfig { get; set; }
    public string CertificateTemplate { get; set; }
    public string RegistrationMarkdown { get; set; }
    public DateTimeOffset RegistrationOpen { get; set; }
    public DateTimeOffset RegistrationClose { get; set; }
    public GameRegistrationType RegistrationType { get; set; }
    public string RegistrationConstraint { get; set; }
    public int MinTeamSize { get; set; } = 1;
    public int MaxTeamSize { get; set; } = 1;
    public int MaxAttempts { get; set; } = 0;
    public int SessionMinutes { get; set; } = 60;
    public int SessionLimit { get; set; } = 0;
    public int GamespaceLimitPerSession { get; set; } = 1;
    public bool IsPublished { get; set; }
    public bool RequireSponsoredTeam { get; set; }
    public bool RequireSynchronizedStart { get; set; } = false;
    public bool AllowPreview { get; set; }
    public bool AllowReset { get; set; }
    public string Key { get; set; }
    public string CardText1 { get; set; }
    public string CardText2 { get; set; }
    public string CardText3 { get; set; }
    public string Mode { get; set; }
    public PlayerMode PlayerMode { get; set; }

    public ICollection<ChallengeSpec> Specs { get; set; } = new List<ChallengeSpec>();
    public ICollection<Player> Players { get; set; } = new List<Player>();
    public ICollection<Challenge> Challenges { get; set; } = new List<Challenge>();
    public ICollection<ChallengeGate> Prerequisites { get; set; } = new List<ChallengeGate>();
    public ICollection<Feedback> Feedback { get; set; } = new List<Feedback>();

    [NotMapped] public bool RequireSession => SessionMinutes > 0;
    [NotMapped] public bool RequireTeam => MinTeamSize > 1;
    [NotMapped] public bool AllowTeam => MaxTeamSize > 1;

    [NotMapped]
    public bool IsLive =>
        GameStart != DateTimeOffset.MinValue &&
        GameStart.CompareTo(DateTimeOffset.UtcNow) < 0 &&
        GameEnd.CompareTo(DateTimeOffset.UtcNow) > 0
    ;
    [NotMapped]
    public bool HasEnded =>
        GameEnd.CompareTo(DateTimeOffset.UtcNow) < 0;

    [NotMapped]
    public bool RegistrationActive =>
        RegistrationType != GameRegistrationType.None &&
        RegistrationOpen.CompareTo(DateTimeOffset.UtcNow) < 0 &&
        RegistrationClose.CompareTo(DateTimeOffset.UtcNow) > 0;

    [NotMapped] public bool IsCompetitionMode => PlayerMode == PlayerMode.Competition;
    [NotMapped] public bool IsPracticeMode => PlayerMode == PlayerMode.Practice;
}
