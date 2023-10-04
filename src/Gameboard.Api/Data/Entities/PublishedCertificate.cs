using System;

namespace Gameboard.Api.Data;

public enum PublishedCertificateMode
{
    Competitive = 0,
    Practice = 1
}

public abstract class PublishedCertificate : IEntity
{
    public string Id { get; set; }
    public DateTimeOffset PublishedOn { get; set; }
    public PublishedCertificateMode Mode { get; set; }

    // navigation
    public string OwnerUserId { get; set; }
    public User OwnerUser { get; set; }
}

public class PublishedPracticeCertificate : PublishedCertificate, IEntity
{
    public string ChallengeSpecId { get; set; }
    public ChallengeSpec ChallengeSpec { get; set; }
}

public class PublishedCompetitiveCertificate : PublishedCertificate, IEntity
{
    public string GameId { get; set; }
    public Game Game { get; set; }
}
