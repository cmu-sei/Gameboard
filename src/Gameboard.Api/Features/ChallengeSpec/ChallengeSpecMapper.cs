// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Collections.Generic;
using System.Text.RegularExpressions;
using Alloy.Api.Client;
using AutoMapper;
using Gameboard.Api.Data;
using TopoMojo.Api.Client;

namespace Gameboard.Api.Services
{
    public partial class ChallengeSpecMapper : Profile
    {
        public ChallengeSpecMapper()
        {
            CreateMap<string, string>().ConvertUsing(str => str == null ? null : str.Trim());
            CreateMap<Data.ChallengeSpec, ChallengeSpec>();
            CreateMap<Data.ChallengeSpec, BoardSpec>();
            CreateMap<Data.ChallengeSpec, ChallengeSpecSummary>()
                .ForMember(d => d.Tags, opt => opt.MapFrom(s => StringTagsToEnumerableStringTags(s.Tags)));
            CreateMap<Data.ChallengeSpec, SimpleEntity>();

            CreateMap<NewChallengeSpec, Data.ChallengeSpec>();
            CreateMap<ChangedChallengeSpec, Data.ChallengeSpec>();

            CreateMap<WorkspaceSummary, ExternalSpec>()
                .ForMember(d => d.ExternalId, opt => opt.MapFrom(s => s.Id))
                .ForMember(d => d.GameEngineType, opt => opt.MapFrom(s => GameEngineType.TopoMojo));

            CreateMap<EventTemplate, ExternalSpec>()
                .ForMember(d => d.ExternalId, opt => opt.MapFrom(s => s.Id))
                .ForMember(d => d.GameEngineType, opt => opt.MapFrom(s => GameEngineType.Crucible));
        }

        [GeneratedRegex("\\s+")]
        private static partial Regex TagsSplitRegex();

        // EF advises to make this mapping a static method to avoid memory leaks
        public static IEnumerable<string> StringTagsToEnumerableStringTags(string tagsIn)
        {
            if (tagsIn.IsEmpty())
                return [];

            return TagsSplitRegex().Split(tagsIn);
        }
    }
}
