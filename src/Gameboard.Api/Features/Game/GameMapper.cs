// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using AutoMapper;


namespace Gameboard.Api.Services
{
    public class GameMapper : Profile
    {
        public GameMapper()
        {
            CreateMap<string, string>().ConvertUsing(str => str == null ? null : str.Trim());

            CreateMap<Data.Game, Game>();

            CreateMap<Data.Game, BoardGame>();

            CreateMap<Game, Data.Game>();

            CreateMap<NewGame, Data.Game>();

            CreateMap<ChangedGame, Data.Game>();

        }
    }
}
