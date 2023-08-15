using AutoMapper;
using Gameboard.Api.Data;

namespace Gameboard.Api.Features.Practice;

internal class PracticeMaps : Profile
{
    public PracticeMaps()
    {
        CreateMap<PracticeModeSettings, UpdatePracticeModeSettings>();
        CreateMap<UpdatePracticeModeSettings, PracticeModeSettings>();
    }
}
