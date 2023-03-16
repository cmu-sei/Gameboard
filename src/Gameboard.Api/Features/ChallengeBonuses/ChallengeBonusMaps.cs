using AutoMapper;
using Gameboard.Api.Data;

namespace Gameboard.Api.Features.ChallengeBonuses;

public class ChallengeBonusMaps : Profile
{
    public ChallengeBonusMaps()
    {
        CreateMap<ManualChallengeBonus, ManualChallengeBonusViewModel>()
            .ForMember(vm => vm.EnteredBy, o => o.MapFrom(m => new UserSimple
            {
                Id = m.EnteredByUserId,
                ApprovedName = m.EnteredBy.ApprovedName
            }));
    }
}
