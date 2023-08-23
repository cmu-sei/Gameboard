using System;
using Gameboard.Api.Common;
using Gameboard.Api.Data;

namespace Gameboard.Api.Features.Certificates;

public sealed class PracticeModeCertificate
{
    public required PracticeModeCertificateChallenge Challenge { get; set; }
    public required string PlayerName { get; set; }
    public required DateTimeOffset Date { get; set; }
    public required double Score { get; set; }
    public required TimeSpan Time { get; set; }
    public required PracticeModeCertificateGame Game { get; set; }
    public required DateTimeOffset? PublishedOn { get; set; }
}

public sealed class PracticeModeCertificateChallenge
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required string Description { get; set; }
    public required string ChallengeSpecId { get; set; }
}

public sealed class PracticeModeCertificateGame
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required string Division { get; set; }
    public required string Season { get; set; }
    public required string Track { get; set; }
}

public class PublishedCertificateViewModel
{
    public required string Id { get; set; }
    public required DateTimeOffset? PublishedOn { get; set; }
    public required PublishedCertificateMode Mode { get; set; }
    public required SimpleEntity AwardedForEntity { get; set; }
    public required SimpleEntity OwnerUser { get; set; }
}
