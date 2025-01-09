using System;

namespace Gameboard.Api.Features.Games;

public sealed class ExportGameResult
{
    public required string ExportBatchId { get; set; }
    public required string GameId { get; set; }
    public required GameImportExport GameData { get; set; }
}

public sealed class GameImportExport
{
    public required string ExportedGameId { get; set; }
    public required string Name { get; set; }
    public required string Competition { get; set; }
    public required string Season { get; set; }
    public required string Track { get; set; }
    public required string Division { get; set; }
    public required string Logo { get; set; }
    public required string Sponsor { get; set; }
    public required string Background { get; set; }
    public required string TestCode { get; set; }
    public required DateTimeOffset? GameStart { get; set; }
    public required DateTimeOffset? GameEnd { get; set; }
    public required string GameMarkdown { get; set; }
    public required string FeedbackConfig { get; set; }
    public required string RegistrationMarkdown { get; set; }
    public required DateTimeOffset? RegistrationOpen { get; set; }
    public required DateTimeOffset? RegistrationClose { get; set; }
    public required GameRegistrationType RegistrationType { get; set; }
    public required int MinTeamSize { get; set; } = 1;
    public required int MaxTeamSize { get; set; } = 1;
    public required int? MaxAttempts { get; set; } = 0;
    public required bool RequireSponsoredTeam { get; set; }
    public required int SessionMinutes { get; set; } = 60;
    public required int? SessionLimit { get; set; } = 0;
    public required int? SessionAvailabilityWarningThreshold { get; set; }
    public required int GamespaceLimitPerSession { get; set; } = 1;
    public required bool IsPublished { get; set; }
    public required bool AllowLateStart { get; set; }
    public required bool AllowPreview { get; set; }
    public required bool AllowPublicScoreboardAccess { get; set; }
    public required bool AllowReset { get; set; }
    public required string CardText1 { get; set; }
    public required string CardText2 { get; set; }
    public required string CardText3 { get; set; }
    public required bool IsFeatured { get; set; }

    // mode stuff
    public string ExternalHostId { get; set; }
    public GameImportExportExternalHost ExternalHost { get; set; }
    public string Mode { get; set; }
    public PlayerMode PlayerMode { get; set; }
    public bool RequireSynchronizedStart { get; set; } = false;
    public bool ShowOnHomePageInPracticeMode { get; set; } = false;

    // feedback
    // public string ChallengesFeedbackTemplateId { get; set; }
    // public FeedbackTemplate ChallengesFeedbackTemplate { get; set; }
    // public string FeedbackTemplateId { get; set; }
    // public FeedbackTemplate FeedbackTemplate { get; set; }

    // public string CertificateTemplateId { get; set; }
    // public CertificateTemplate CertificateTemplate { get; set; }
    // public string PracticeCertificateTemplateId { get; set; }
    // public CertificateTemplate PracticeCertificateTemplate { get; set; }
}

public sealed class GameImportExportExternalHost
{
    public required string ExportedId { get; set; }
}

public sealed class GameImportExportFeedbackTemplate
{
    public required string ExportedFeedbackTemplateId { get; set; }
}
