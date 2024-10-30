using System;

namespace Gameboard.Api.Data;

public class PracticeModeSettings : IEntity
{
    public string Id { get; set; }
    public int? AttemptLimit { get; set; }
    public string CertificateHtmlTemplate { get; set; }
    public int DefaultPracticeSessionLengthMinutes { get; set; }
    public string IntroTextMarkdown { get; set; }
    public int? MaxConcurrentPracticeSessions { get; set; }
    public int? MaxPracticeSessionLengthMinutes { get; set; }
    public string SuggestedSearches { get; set; }
    public DateTimeOffset? UpdatedOn { get; set; }

    public User UpdatedByUser { get; set; }
    public string UpdatedByUserId { get; set; }
}
