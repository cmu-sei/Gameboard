// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using AutoMapper;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Gameboard.Api.Services
{
    public class GameMapper : Profile
    {
        public GameMapper()
        {
            var yaml = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .IgnoreUnmatchedProperties()
                .Build();

            CreateMap<string, string>().ConvertUsing(str => str == null ? null : str.Trim());

            CreateMap<Data.Game, Game>()
                .ForMember(d => d.FeedbackTemplate, opt => opt.MapFrom(s =>
                    yaml.Deserialize<BoardFeedbackTemplate>(s.FeedbackConfig ?? "")
                ));

            CreateMap<Data.Game, BoardGame>()
                .ForMember(d => d.FeedbackTemplate, opt => opt.MapFrom(s =>
                    yaml.Deserialize<BoardFeedbackTemplate>(s.FeedbackConfig)
                ));

            CreateMap<Game, Data.Game>();

            CreateMap<NewGame, Data.Game>();

            CreateMap<ChangedGame, Data.Game>();

        }
    }
}
