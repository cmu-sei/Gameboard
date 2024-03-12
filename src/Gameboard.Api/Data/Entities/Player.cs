// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gameboard.Api.Data;

public class Player : IEntity
{
    public string Id { get; set; }
    public string TeamId { get; set; }
    public string UserId { get; set; }
    public string GameId { get; set; }
    public string ApprovedName { get; set; }
    public string Name { get; set; }
    public string NameStatus { get; set; }
    public string InviteCode { get; set; }
    public bool IsReady { get; set; }
    public PlayerRole Role { get; set; }
    public DateTimeOffset SessionBegin { get; set; }
    public DateTimeOffset SessionEnd { get; set; }
    public double SessionMinutes { get; set; }
    public int Rank { get; set; }
    public int Score { get; set; }
    public long Time { get; set; }
    public int CorrectCount { get; set; }
    public int PartialCount { get; set; }
    public bool Advanced { get; set; }
    public PlayerMode Mode { get; set; }
    public User User { get; set; }
    public Game Game { get; set; }
    public DateTimeOffset WhenCreated { get; set; }
    public bool IsLateStart { get; set; }

    // advancement tracking
    public string AdvancedFromGameId { get; set; }
    public Game AdvancedFromGame { get; set; }
    public string AdvancedFromPlayerId { get; set; }
    public Player AdvancedFromPlayer { get; set; }
    public ICollection<Player> AdvancedToPlayers { get; set; }
    public string AdvancedFromTeamId { get; set; }
    public double? AdvancedWithScore { get; set; }

    // other navigation properties
    public ICollection<Challenge> Challenges { get; set; } = new List<Challenge>();
    public string SponsorId { get; set; }
    public Sponsor Sponsor { get; set; }

    [NotMapped] public bool IsManager => Role == PlayerRole.Manager;
    [NotMapped] public bool IsPractice => Mode == PlayerMode.Practice;
    [NotMapped] public bool IsCompetition => Mode == PlayerMode.Competition;
    [NotMapped]
    public bool IsLive =>
        SessionBegin > DateTimeOffset.MinValue &&
        SessionBegin < DateTimeOffset.UtcNow &&
        SessionEnd > DateTimeOffset.UtcNow;

    // Control delete behavior with relationships
    public ICollection<Feedback> Feedback { get; set; } = new List<Feedback>();
    public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
}
