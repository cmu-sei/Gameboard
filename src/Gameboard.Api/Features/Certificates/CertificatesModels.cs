using System;

namespace Gameboard.Api.Features.Certificates;

public sealed class PracticeModeCertificate
{
    public required PracticeModeCertificateChallenge Challenge { get; set; }
    public required string PlayerName { get; set; }
    public required DateTimeOffset Date { get; set; }
    public required double Score { get; set; }
    public required TimeSpan Time { get; set; }
    public required PracticeModeCertificateGame Game { get; set; }
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
