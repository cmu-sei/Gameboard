using System;
using System.Collections.Generic;

namespace Gameboard.Api.Features.Games;

public sealed class GameImportExportBatch
{
    public required string ExportBatchId { get; set; }
    public required GameImportExportGame[] Games { get; set; }
    public required IDictionary<string, GameImportExportCertificateTemplate> CertificateTemplates { get; set; }
    public required IDictionary<string, GameImportExportExternalHost> ExternalHosts { get; set; }
    public required IDictionary<string, GameImportExportFeedbackTemplate> FeedbackTemplates { get; set; }
    public required IDictionary<string, GameImportExportSponsor> Sponsors { get; set; }
}

public sealed class GameImportExportGame
{
    public required string GameId { get; set; }
    public required string Name { get; set; }
    public required string Competition { get; set; }
    public required string Season { get; set; }
    public required string Track { get; set; }
    public required string Division { get; set; }
    public required string Logo { get; set; }
    public required string Sponsor { get; set; }
    public required string Background { get; set; }
    public required DateTimeOffset? GameStart { get; set; }
    public required DateTimeOffset? GameEnd { get; set; }
    public required string GameMarkdown { get; set; }
    public required string RegistrationMarkdown { get; set; }
    public required DateTimeOffset? RegistrationOpen { get; set; }
    public required DateTimeOffset? RegistrationClose { get; set; }
    public required GameRegistrationType RegistrationType { get; set; }
    public required int MinTeamSize { get; set; }
    public required int MaxTeamSize { get; set; }
    public required int? MaxAttempts { get; set; }
    public required bool RequireSponsoredTeam { get; set; }
    public required int SessionMinutes { get; set; }
    public required int? SessionLimit { get; set; }
    public required int? SessionAvailabilityWarningThreshold { get; set; }
    public required int GamespaceLimitPerSession { get; set; }
    public required bool IsPublished { get; set; }
    public required bool AllowLateStart { get; set; }
    public required bool AllowPreview { get; set; }
    public required bool AllowPublicScoreboardAccess { get; set; }
    public required bool AllowReset { get; set; }
    public required string CardText1 { get; set; }
    public required string CardText2 { get; set; }
    public required string CardText3 { get; set; }
    public required bool IsFeatured { get; set; }
    public required string Mode { get; set; }
    public required PlayerMode PlayerMode { get; set; }
    public required bool RequireSynchronizedStart { get; set; }
    public required bool ShowOnHomePageInPracticeMode { get; set; }

    public required string ExternalHostExportId { get; set; }
    public required string CertificateTemplateExportId { get; set; }
    public required string PracticeCertificateTemplateExportId { get; set; }
    public required string ChallengesFeedbackTemplateExportId { get; set; }
    public required string FeedbackTemplateExportId { get; set; }
}

public sealed class GameImportExportExternalHost
{
    public required string ExportId { get; set; }
}

public sealed class GameImportExportFeedbackTemplate
{
    public required string ExportId { get; set; }
}

public sealed class GameImportExportCertificateTemplate
{
    public required string ExportId { get; set; }
}

public sealed class GameImportExportSponsor
{
    public required string ExportId { get; set; }
}

public sealed class ImportedGame
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required string ExportId { get; set; }
}
