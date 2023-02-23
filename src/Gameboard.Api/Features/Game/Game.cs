// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Gameboard.Api
{
    public class GameDetail
    {
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
        public GameFeedbackTemplate FeedbackTemplate { get; set; }
        public string CertificateTemplate { get; set; }
        public string RegistrationMarkdown { get; set; }
        public DateTimeOffset RegistrationOpen { get; set; }
        public DateTimeOffset RegistrationClose { get; set; }
        public GameRegistrationType RegistrationType { get; set; }
        public string RegistrationConstraint { get; set; }
        public int MaxAttempts { get; set; }
        public int MinTeamSize { get; set; }
        public int MaxTeamSize { get; set; }
        public int SessionMinutes { get; set; }
        public int SessionLimit { get; set; }
        public int GamespaceLimitPerSession { get; set; }
        public bool IsPublished { get; set; }
        public bool RequireSponsoredTeam { get; set; }
        public bool AllowPreview { get; set; }
        public bool AllowReset { get; set; }
        public string Key { get; set; }
        public string CardText1 { get; set; }
        public string CardText2 { get; set; }
        public string CardText3 { get; set; }
        public string Mode { get; set; }
        public PlayerMode PlayerMode { get; set; }
    }

    public class Game: GameDetail
    {
        public string Id { get; set; }
        public bool RequireSession { get; set; }
        public bool RequireTeam { get; set; }
        public bool AllowTeam { get; set; }
        public bool IsLive { get; set; }
        public bool HasEnded { get; set; }
        public bool RegistrationActive { get; set; }
        public bool IsPracticeMode { get; set; }
    }

    public class NewGame: GameDetail
    {
        public bool IsClone { get; set; } = false;
    }

    public class ChangedGame: Game
    {

    }

    public class GameSearchFilter: SearchFilter
    {
        public const string PastFilter = "past";
        public const string PresentFilter = "present";
        public const string FutureFilter = "future";
        public bool WantsPresent => Filter.Contains(PresentFilter);
        public bool WantsPast => Filter.Contains(PastFilter);
        public bool WantsFuture => Filter.Contains(FutureFilter);
    }

    public class BoardGame
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Competition { get; set; }
        public string Season { get; set; }
        public string Track { get; set; }
        public string Division { get; set; }
        public string Mode { get; set; }
        public string Logo { get; set; }
        public string Sponsor { get; set; }
        public GameFeedbackTemplate FeedbackTemplate { get; set; }
        public string Background { get; set; }
        public bool AllowPreview { get; set; }
        public bool AllowReset { get; set; }
        public bool IsPracticeMode { get; set; }
        public ICollection<BoardSpec> Specs { get; set; } = new List<BoardSpec>();
        public ICollection<ChallengeGate> Prerequisites { get; set; } = new List<ChallengeGate>();

    }

    public class UploadedFile
    {
        public string Filename { get; set; }
    }

    public class SessionForecast
    {
        public DateTimeOffset Time { get; set; }
        public int Available { get; set; }
        public int Reserved { get; set; }
    }

    public class GameGroup
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public Game[] Games { get; set; }
    }
}
