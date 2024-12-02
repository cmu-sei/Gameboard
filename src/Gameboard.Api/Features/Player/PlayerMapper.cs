// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using AutoMapper;
using Gameboard.Api.Features.Teams;

namespace Gameboard.Api.Services;

public class PlayerMapper : Profile
{
    public PlayerMapper()
    {
        CreateMap<string, string>().ConvertUsing(str => str == null ? null : str.Trim());
        CreateMap<Data.Player, Player>();
        CreateMap<Data.Player, BoardPlayer>()
            .AfterMap((s, d) =>
            {
                if (s.Challenges.IsNotEmpty())
                {
                    d.CorrectCount = s.Challenges.Where(c => c.Result == ChallengeResult.Success).Count();
                    d.PartialCount = s.Challenges.Where(c => c.Result == ChallengeResult.Partial).Count();
                }
            });
        CreateMap<Data.Player, SimpleEntity>()
            .ForMember(d => d.Name, o => o.MapFrom(p => p.ApprovedName));
        CreateMap<Data.Player, Standing>();
        CreateMap<Data.Player, TeamPlayer>();
        CreateMap<Data.Player, TeamMember>();
        CreateMap<Data.Player, SimpleEntity>()
            .ForMember(d => d.Name, opts => opts.MapFrom(p => p.ApprovedName));
        CreateMap<TeamPlayer, Data.Player>();
        CreateMap<Data.Player, PlayerOverview>();
        CreateMap<Player, TeamPlayer>();
        CreateMap<Player, Data.Player>();
        CreateMap<NewPlayer, Data.Player>();
        CreateMap<ChangedPlayer, Data.Player>();
        CreateMap<ChangedPlayer, SelfChangedPlayer>();
        CreateMap<SelfChangedPlayer, Data.Player>();

        CreateMap<Player, PlayerUpdatedViewModel>()
            .ForMember(vm => vm.PreUpdateName, opts => opts.MapFrom(p => p.ApprovedName))
            .ForMember(vm => vm.PreUpdateName, opts => opts.Ignore());

        CreateMap<Data.Player, Team>()
            .AfterMap((player, team) =>
            {
                team.Members =
                [
                    new()
                    {
                        Id = player.Id,
                        ApprovedName = player.ApprovedName,
                        Role = player.Role,
                        UserId = player.UserId
                    }
                ];

                if (team.Members.Any() && !team.Members.Any(p => p.Role == PlayerRole.Manager))
                {
                    team.Members.OrderBy(p => p.ApprovedName).First().Role = PlayerRole.Manager;
                }
            });
    }
}
