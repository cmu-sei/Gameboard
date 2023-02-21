// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using Alloy.Api.Client;
using AutoMapper;
using TopoMojo.Api.Client;

namespace Gameboard.Api.Services
{
    public class ChallengeSpecMapper : Profile
    {
        public ChallengeSpecMapper()
        {
            CreateMap<string, string>().ConvertUsing(str => str == null ? null : str.Trim());

            CreateMap<Data.ChallengeSpec, ChallengeSpec>();

            CreateMap<Data.ChallengeSpec, BoardSpec>();

            CreateMap<NewChallengeSpec, Data.ChallengeSpec>();

            CreateMap<ChangedChallengeSpec, Data.ChallengeSpec>();

            CreateMap<WorkspaceSummary, ExternalSpec>()
                .ForMember(d => d.ExternalId, opt => opt.MapFrom(s => s.Id))
                .ForMember(d => d.GameEngineType, opt => opt.MapFrom(s => GameEngineType.TopoMojo))
            ;

            CreateMap<EventTemplate, ExternalSpec>()
                .ForMember(d => d.ExternalId, opt => opt.MapFrom(s => s.Id))
                .ForMember(d => d.GameEngineType, opt => opt.MapFrom(s => GameEngineType.Crucible))
            ;
        }
    }
}
