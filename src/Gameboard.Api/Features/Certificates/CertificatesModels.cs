using System;
using System.Collections.Generic;
using Gameboard.Api.Data;
using Gameboard.Api.Structure;

namespace Gameboard.Api.Features.Certificates;

public sealed class CompetitiveModeCertificate
{
    public required DateTimeOffset Date { get; set; }
    public required string PlayerName { get; set; }
    public required string TeamName { get; set; }
    public required string UserName { get; set; }
    public required CertificateGameView Game { get; set; }
    public required TimeSpan Duration { get; set; }
    public required int? Rank { get; set; }
    public required double Score { get; set; }
    public required int? UniquePlayerCount { get; set; }
    public required int? UniqueTeamCount { get; set; }
    public required DateTimeOffset? PublishedOn { get; set; }
}

public sealed class PracticeModeCertificate
{
    public required PracticeModeCertificateChallenge Challenge { get; set; }
    public required string PlayerName { get; set; }
    public required string TeamName { get; set; }
    public required string UserName { get; set; }
    public required DateTimeOffset Date { get; set; }
    public required double Score { get; set; }
    public required TimeSpan Time { get; set; }
    public required CertificateGameView Game { get; set; }
    public required DateTimeOffset? PublishedOn { get; set; }
}

public sealed class PracticeModeCertificateChallenge
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required string Description { get; set; }
    public required string ChallengeSpecId { get; set; }
}

public sealed class CertificateGameView
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required string Division { get; set; }
    public required string Season { get; set; }
    public required string Series { get; set; }
    public required string Track { get; set; }
    public required bool IsTeamGame { get; set; }
    public required double MaxPossibleScore { get; set; }
}

public class PublishedCertificateViewModel
{
    public required string Id { get; set; }
    public required DateTimeOffset? PublishedOn { get; set; }
    public required PublishedCertificateMode Mode { get; set; }
    public required SimpleEntity AwardedForEntity { get; set; }
    public required SimpleEntity OwnerUser { get; set; }
}

public sealed class CertificateTemplateView
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required string Content { get; set; }
    public required SimpleEntity CreatedByUser { get; set; }
    public required IEnumerable<SimpleEntity> UseAsTemplateForGames { get; set; }
    public required IEnumerable<SimpleEntity> UseAsPracticeTemplateForGames { get; set; }
}

public sealed class CertificateHtmlContext
{
    public CertificateHtmlContextChallenge Challenge { get; set; }
    public CertificateHtmlContextGame Game { get; set; }
    public required string PlayerName { get; set; }
    public required string TeamName { get; set; }
    public required string UserId { get; set; }
    public required string UserName { get; set; }
    public required string UserRequestedName { get; set; }

    public required DateTimeOffset Date { get; set; }
    public required TimeSpan Duration { get; set; }
    public int? Rank { get; set; }
    public required double Score { get; set; }
    public int? TotalPlayerCount { get; set; }
    public int? TotalTeamCount { get; set; }
}

public sealed class CertificateHtmlContextChallenge
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required string Description { get; set; }
}

public sealed class CertificateHtmlContextGame
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required string Division { get; set; }
    public required string Season { get; set; }
    public required string Series { get; set; }
    public required string Track { get; set; }
}

public sealed class NoCertificateTemplateConfigured : GameboardValidationException
{
    public NoCertificateTemplateConfigured(string entityId)
        : base($"No certificate has been configured for entity {entityId}") { }
}
