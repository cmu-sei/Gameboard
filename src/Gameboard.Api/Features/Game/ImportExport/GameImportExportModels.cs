using System;
using System.Collections.Generic;

namespace Gameboard.Api.Features.Games;

public sealed class GameImportExportBatch
{
    public required string ExportBatchId { get; set; }
    public required string DownloadUrl { get; set; }
    public required GameImportExportGame[] Games { get; set; }
    public required string PracticeAreaCertificateTemplateId { get; set; }
    public required IDictionary<string, GameImportExportCertificateTemplate> CertificateTemplates { get; set; }
    public required IDictionary<string, GameImportExportExternalHost> ExternalHosts { get; set; }
    public required IDictionary<string, GameImportExportFeedbackTemplate> FeedbackTemplates { get; set; }
    public required IDictionary<string, GameImportExportSponsor> Sponsors { get; set; }
}

public sealed class GameImportExportGame
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required string Competition { get; set; }
    public required string Season { get; set; }
    public required string Track { get; set; }
    public required string Division { get; set; }
    public required string CardImageFileName { get; set; }
    public required string SponsorId { get; set; }
    public required DateTimeOffset? GameStart { get; set; }
    public required DateTimeOffset? GameEnd { get; set; }
    public required string GameMarkdown { get; set; }
    public required string RegistrationConstraint { get; set; }
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
    public required string MapImageFileName { get; set; }
    public required string Mode { get; set; }
    public required PlayerMode PlayerMode { get; set; }
    public required bool RequireSynchronizedStart { get; set; }
    public required bool ShowOnHomePageInPracticeMode { get; set; }

    public required string CertificateTemplateId { get; set; }
    public required string ChallengesFeedbackTemplateId { get; set; }
    public required GameImportExportChallengeSpec[] Specs { get; set; }
    public required string ExternalHostId { get; set; }
    public required string FeedbackTemplateId { get; set; }
    public required string PracticeCertificateTemplateId { get; set; }
}

public sealed class GameImportExportChallengeSpec
{
    public required string Description { get; set; }
    public required bool Disabled { get; set; }
    public required string ExternalId { get; set; }
    public required GameEngineType GameEngineType { get; set; }
    public required bool IsHidden { get; set; }
    public required string Name { get; set; }
    public required int Points { get; set; }
    public required bool ShowSolutionGuideInCompetitiveMode { get; set; }
    public required string Tag { get; set; }
    public required string Tags { get; set; }
    public required string Text { get; set; }
    public required float X { get; set; }
    public required float Y { get; set; }
    public required float R { get; set; }
}

public sealed class GameImportExportExternalHost
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required string ClientUrl { get; set; }
    public required bool DestroyResourcesOnDeployFailure { get; set; }
    public required int? GamespaceDeployBatchSize { get; set; }
    public required int? HttpTimeoutInSeconds { get; set; }
    public required string HostUrl { get; set; }
    public required string PingEndpoint { get; set; }
    public required string StartupEndpoint { get; set; }
    public required string TeamExtendedEndpoint { get; set; }
}

public sealed class GameImportExportFeedbackTemplate
{
    public string Id { get; set; }
    public string HelpText { get; set; }
    public required string Name { get; set; }
    public required string Content { get; set; }
}

public sealed class GameImportExportCertificateTemplate
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Content { get; set; }
}

public sealed class GameImportExportSponsor
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required string LogoFileName { get; set; }
    public required bool Approved { get; set; }
    public required GameImportExportSponsor ParentSponsor { get; set; }
}

public sealed class ImportedGame
{
    public required string Id { get; set; }
    public required string Name { get; set; }
}

public sealed class ExportPackageNotFound : GameboardException
{
    public ExportPackageNotFound(string exportBatchId) : base($"Export package {exportBatchId} doesn't exist.") { }
}

[AttributeUsage(AttributeTargets.Property)]
public sealed class DontExportAttribute : Attribute { }
