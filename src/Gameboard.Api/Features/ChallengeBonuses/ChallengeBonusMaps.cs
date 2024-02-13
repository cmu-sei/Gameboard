using AutoMapper;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Scores;

namespace Gameboard.Api.Features.ChallengeBonuses;

public class ChallengeBonusMaps : Profile
{
    public ChallengeBonusMaps()
    {
        CreateMap<ManualBonus, ManualChallengeBonusViewModel>()
            .ForMember(vm => vm.EnteredBy, o => o.MapFrom(m => new SimpleEntity
            {
                Id = m.EnteredByUserId,
                Name = m.EnteredByUser.ApprovedName
            }));

        CreateMap<Data.AwardedChallengeBonus, GameScoreAutoChallengeBonus>();
        CreateMap<Data.ChallengeBonus, GameScoringConfigChallengeBonus>();
        CreateMap<Data.ChallengeBonus, GameScoreAutoChallengeBonus>();
        CreateMap<Data.ChallengeBonusCompleteSolveRank, GameScoringConfigChallengeBonus>();
    }
}
