// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using AutoMapper;
using Gameboard.Api.Features.Common;
using Gameboard.Api.Hubs;

namespace Gameboard.Api.Services
{
    public class UserMapper : Profile
    {
        public UserMapper()
        {
            CreateMap<string, string>().ConvertUsing(str => str == null ? null : str.Trim());

            CreateMap<Data.User, User>();
            CreateMap<Data.User, HubEventActingUserDescription>();
            CreateMap<Data.User, TeamMember>();
            CreateMap<Data.User, UserSimple>();
            CreateMap<Data.User, UserOnly>();
            CreateMap<Data.User, SimpleEntity>()
                .ForMember(s => s.Name, opt => opt.MapFrom(u => u.ApprovedName));
            CreateMap<User, Data.User>();
            CreateMap<NewUser, Data.User>();
            CreateMap<ChangedUser, SelfChangedUser>();
            CreateMap<ChangedUser, Data.User>();
            CreateMap<SelfChangedUser, Data.User>();
        }
    }
}
