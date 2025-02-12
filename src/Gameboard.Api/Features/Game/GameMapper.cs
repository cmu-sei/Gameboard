// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Linq;
using AutoMapper;
using Gameboard.Api.Features.Feedback;
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

            // Use BeforeMap for custom mapping since Deserialize() could error and prevent Game from loading
            // Need to not throw error if format is invalid. If Deserialize() could default to null on error, that would be ideal
            CreateMap<Data.Game, Game>()
                .BeforeMap((src, dest) =>
                {
                    try
                    {
                        dest.FeedbackTemplate = yaml.Deserialize<GameFeedbackTemplate>(src.FeedbackConfig ?? "");
                    }
                    catch
                    {
                        dest.FeedbackTemplate = null;
                    }
                })
                .ForMember(d => d.CountPlayers, o => o.MapFrom(s => s.Players.Select(p => p.UserId).Distinct().Count()))
                .ForMember(d => d.CountTeams, o => o.MapFrom(s => s.Players.Select(p => p.TeamId).Distinct().Count()));

            CreateMap<Data.Game, BoardGame>()
                .BeforeMap((src, dest) =>
                {
                    try
                    {
                        dest.FeedbackTemplate = yaml.Deserialize<GameFeedbackTemplate>(src.FeedbackConfig ?? "");
                    }
                    catch
                    {
                        dest.FeedbackTemplate = null;
                    }
                });

            CreateMap<Game, Data.Game>();
            CreateMap<NewGame, Data.Game>();
            CreateMap<ChangedGame, Data.Game>()
                .ForMember(d => d.FeedbackTemplateId, opt => opt.MapFrom(s => s.FeedbackTemplateId))
                .ForMember(d => d.ChallengesFeedbackTemplateId, opt => opt.MapFrom(s => s.ChallengesFeedbackTemplateId))
                .ForMember(d => d.FeedbackTemplate, opt => opt.Ignore())
                .ForMember(d => d.ChallengesFeedbackTemplate, opt => opt.Ignore());

            // FROM Data.Game
            CreateMap<Data.Game, SimpleEntity>();
        }
    }
}
