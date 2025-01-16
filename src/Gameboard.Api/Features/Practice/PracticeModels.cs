using System;
using System.Collections.Generic;

namespace Gameboard.Api.Features.Practice;

public sealed class AutoExtendPracticeSessionResult
{
    public required bool IsExtended { get; set; }
    public required DateTimeOffset SessionEnd { get; set; }
}

public sealed class PracticeSession
{
    public required string GameId { get; set; }
    // would really love not to need PlayerId right now, but starting challenges with a teamId is
    // a big effort, as it happens
    public required string PlayerId { get; set; }
    public required TimestampRange Session { get; set; }
    public required string TeamId { get; set; }
    public required string UserId { get; set; }
}

public sealed class PracticeModeSettingsApiModel
{
    public int? AttemptLimit { get; set; }
    public string CertificateTemplateId { get; set; }
    public required int DefaultPracticeSessionLengthMinutes { get; set; }
    public required string IntroTextMarkdown { get; set; }
    public int? MaxConcurrentPracticeSessions { get; set; }
    public int? MaxPracticeSessionLengthMinutes { get; set; }
    public required IEnumerable<string> SuggestedSearches { get; set; }
}
