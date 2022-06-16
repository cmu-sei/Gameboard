// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using AutoMapper;

namespace Gameboard.Api.Services
{
    public class PlayerMapper : Profile
    {
        public PlayerMapper()
        {
            CreateMap<string, string>().ConvertUsing(str => str == null ? null : str.Trim());

            CreateMap<Data.Player, Player>();

            CreateMap<Data.Player, BoardPlayer>();

            CreateMap<Data.Player, Standing>();

            CreateMap<Data.Player, Team>();

            CreateMap<Data.Player, TeamPlayer>();

            CreateMap<Data.Player, PlayerOverview>();

            CreateMap<Player, TeamPlayer>();

            CreateMap<Player, TeamState>();

            CreateMap<Player, Data.Player>();

            CreateMap<NewPlayer, Data.Player>();

            CreateMap<ChangedPlayer, Data.Player>();

            CreateMap<ChangedPlayer, SelfChangedPlayer>();

            CreateMap<SelfChangedPlayer, Data.Player>();
        }
    }
}
