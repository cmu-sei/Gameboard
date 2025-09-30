// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

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
