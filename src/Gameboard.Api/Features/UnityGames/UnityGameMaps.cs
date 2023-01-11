using AutoMapper;
using Gameboard.Api.Features.UnityGames.ViewModels;

namespace Gameboard.Api.Features.UnityGames;

public class UnityGameMaps : Profile
{
    public UnityGameMaps()
    {
        CreateMap<Gameboard.Api.Data.Challenge, UnityGameChallengeViewModel>()
            .ForMember(vm => vm.GraderKey, opt => opt.Ignore());
    }
}