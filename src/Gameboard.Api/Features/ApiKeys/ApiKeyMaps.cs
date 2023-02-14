using AutoMapper;
using Gameboard.Api.Features.ApiKeys;

public class ApiKeyMaps : Profile
{
    public ApiKeyMaps()
    {
        CreateMap<Gameboard.Api.Data.ApiKey, ApiKeyViewModel>()
            .ForSourceMember(k => k.Key, opt => opt.DoNotValidate());

        CreateMap<Gameboard.Api.Data.ApiKey, CreateApiKeyResult>()
            .ForSourceMember(k => k.Key, opt => opt.DoNotValidate())
            .ForMember(r => r.PlainKey, opt => opt.Ignore());
    }
}
