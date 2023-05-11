using AutoMapper;
using Gameboard.Api.Data;
using Gameboard.Api.Structure;

namespace Gameboard.Api.Features.ChallengeBonuses;

public class ChallengeBonusMaps : Profile
{
    public ChallengeBonusMaps()
    {
        CreateMap<ManualChallengeBonus, ManualChallengeBonusViewModel>()
            .ForMember(vm => vm.EnteredBy, o => o.MapFrom(m => new SimpleEntity
            {
                Id = m.EnteredByUserId,
                Name = m.EnteredByUser.ApprovedName
            }));

        CreateMap<Data.ChallengeBonus, GameScoringConfigChallengeBonus>();
        CreateMap<Data.ChallengeBonusSolveSpeed, GameScoringConfigChallengeBonus>();
        CreateMap<Data.AwardedChallengeBonus, GameScoreAwardedChallengeBonus>();
    }
}
