using Gameboard.Api.Common;

namespace Gameboard.Api.Features.Practice;

public sealed class SearchPracticeChallengesResult
{
    public required PagedEnumerable<ChallengeSpecSummary> Results { get; set; }
}

public sealed class UpdatePracticeModeSettings
{
    public required string CertificateHtmlTemplate { get; set; }
    public required int DefaultPracticeSessionLengthMinutes { get; set; }
    public required string IntroTextMarkdown { get; set; }
    public int? MaxConcurrentPracticeSessions { get; set; }
    public int? MaxPracticeSessionLengthMinutes { get; set; }
}
