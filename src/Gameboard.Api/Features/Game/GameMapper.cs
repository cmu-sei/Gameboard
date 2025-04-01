// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Linq;
using AutoMapper;

namespace Gameboard.Api.Services;

public class GameMapper : Profile
{
    public GameMapper()
    {
        CreateMap<string, string>().ConvertUsing(str => str == null ? null : str.Trim());
        CreateMap<Game, Data.Game>();
        CreateMap<GameDetail, Data.Game>();
        CreateMap<ChangedGame, Data.Game>()
            .ForMember(d => d.FeedbackTemplateId, opt => opt.MapFrom(s => s.FeedbackTemplateId))
            .ForMember(d => d.ChallengesFeedbackTemplateId, opt => opt.MapFrom(s => s.ChallengesFeedbackTemplateId))
            .ForMember(d => d.FeedbackTemplate, opt => opt.Ignore())
            .ForMember(d => d.ChallengesFeedbackTemplate, opt => opt.Ignore());

        // FROM Data.Game
        CreateMap<Data.Game, BoardGame>();
        CreateMap<Data.Game, Game>()
            .ForMember(d => d.CountPlayers, o => o.MapFrom(s => s.Players.Select(p => p.UserId).Distinct().Count()))
            .ForMember(d => d.CountTeams, o => o.MapFrom(s => s.Players.Select(p => p.TeamId).Distinct().Count()));
        CreateMap<Data.Game, SimpleEntity>();
    }
}
