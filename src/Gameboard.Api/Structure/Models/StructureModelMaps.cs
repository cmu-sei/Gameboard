using AutoMapper;

namespace Gameboard.Api.Structure;

internal class StructureModelMaps : Profile
{
    public StructureModelMaps()
    {
        CreateMap<SimpleEntity, string>()
            .ForAllMembers(d => d.MapFrom(s => s.Name));
    }
}
