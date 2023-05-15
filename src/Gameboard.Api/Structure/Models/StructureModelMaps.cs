using AutoMapper;

namespace Gameboard.Api.Structure;

internal class StructureModelMaps : Profile
{
    public StructureModelMaps()
    {
        CreateMap<SimpleEntity, string>()
            .ConvertUsing(se => se != null ? se.Name : string.Empty);
    }
}
