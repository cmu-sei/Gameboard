using AutoMapper;

namespace Gameboard.Api.Features.Scores;

public sealed class ScoringMaps : Profile
{
    public ScoringMaps()
    {
        CreateMap<TeamChallengeScoreSummary, TeamChallengeScore>();
    }
}
