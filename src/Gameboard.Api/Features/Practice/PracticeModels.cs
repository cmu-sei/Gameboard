using Gameboard.Api.Common;

namespace Gameboard.Api.Features.Practice;

public sealed class SearchPracticeChallengesResult
{
    public required PagedEnumerable<ChallengeSpecSummary> Results { get; set; }
}
