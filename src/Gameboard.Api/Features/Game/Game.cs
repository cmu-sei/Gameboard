// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Gameboard.Api.Features.Feedback;

namespace Gameboard.Api;

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
    public DateTimeOffset GameStart { get; set; }
    public DateTimeOffset GameEnd { get; set; }
    public string GameMarkdown { get; set; }
    public string FeedbackConfig { get; set; }
    public GameFeedbackTemplate FeedbackTemplate { get; set; }
    public string ChallengesFeedbackTemplateId { get; set; }
    public string FeedbackTemplateId { get; set; }
    public string CertificateTemplateId { get; set; }
    public string PracticeCertificateTemplateId { get; set; }
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
    public int? SessionAvailabilityWarningThreshold { get; set; }
    public int GamespaceLimitPerSession { get; set; }
    public string ExternalHostId { get; set; }
    public bool IsPublished { get; set; }
    public bool RequireSponsoredTeam { get; set; }
    public bool RequireSynchronizedStart { get; set; }
    public bool AllowLateStart { get; set; }
    public bool AllowPublicScoreboardAccess { get; set; }
    public bool AllowPreview { get; set; }
    public bool AllowReset { get; set; }
    public string CardText1 { get; set; }
    public string CardText2 { get; set; }
    public string CardText3 { get; set; }
    public string Mode { get; set; }
    public bool IsFeatured { get; set; }
    public PlayerMode PlayerMode { get; set; }
    public bool ShowOnHomePageInPracticeMode { get; set; }
}

public class Game : GameDetail
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

public class NewGame : GameDetail
{
    public bool IsClone { get; set; } = false;
}

public class ChangedGame : Game { }

public class GameSearchFilter : SearchFilter
{
    private const string AdvanceableFilter = "advanceable";
    private const string CompetitiveFilter = "competitive";
    private const string PracticeFilter = "practice";
    private const string PastFilter = "past";
    private const string PresentFilter = "present";
    private const string FutureFilter = "future";

    public bool? IsFeatured { get; set; }
    public bool? IsOngoing { get; set; }

    public bool WantsAdvanceable => Filter.Contains(AdvanceableFilter);
    public bool WantsCompetitive => Filter.Contains(CompetitiveFilter);
    public bool WantsPractice => Filter.Contains(PracticeFilter);
    public bool WantsPresent => Filter.Contains(PresentFilter);
    public bool WantsPast => Filter.Contains(PastFilter);
    public bool WantsFuture => Filter.Contains(FutureFilter);

    public string OrderBy { get; set; }
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
    public string GameFeedbackTemplateId { get; set; }
    public string ChallengesFeedbackTemplateId { get; set; }
    public GameFeedbackTemplate FeedbackTemplate { get; set; }
    public string Background { get; set; }
    public bool AllowPreview { get; set; }
    public bool AllowReset { get; set; }
    public bool IsPracticeMode { get; set; }
    public ICollection<BoardSpec> Specs { get; set; } = new List<BoardSpec>();
    public ICollection<ChallengeGate> Prerequisites { get; set; } = new List<ChallengeGate>();
}

public sealed class GameSearchQuery
{
    public bool? PlayerMode { get; set; }
    public string SearchTerm { get; set; }
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

public class GameEngineMode
{
    public static readonly string External = "external";
    public static readonly string Standard = "vm";
}

public class GameGroup
{
    public int Year { get; set; }
    public int Month { get; set; }
    public Game[] Games { get; set; }
}

public sealed record DeployGameResourcesBody(IEnumerable<string> TeamIds);

public sealed class GameActiveTeam
{
    public required string TeamId { get; set; }
    public required DateTimeOffset SessionEnd { get; set; }
}

public sealed class GameImportExport
{
    public required string CardImageUrl { get; set; }
    public required string MapImageUrl { get; set; }
    public required Data.Game Game { get; set; }
}
