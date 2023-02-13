using AutoMapper;
using Gameboard.Api.Features.ApiKeys;

public class ApiKeyMaps : Profile
{
    public ApiKeyMaps()
    {
        CreateMap<Gameboard.Api.Data.ApiKey, CreateApiKeyResult>()
            .ForSourceMember(k => k.Key, opt => opt.DoNotValidate())
            .ForMember(r => r.UnhashedKey, opt => opt.Ignore());
    }
}
