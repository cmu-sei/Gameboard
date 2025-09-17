// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using AutoMapper;

namespace Gameboard.Api.Services
{
    public class UserMapper : Profile
    {
        public UserMapper()
        {
            CreateMap<string, string>().ConvertUsing(str => str == null ? null : str.Trim());

            // FROM Data.User
            CreateMap<Data.User, User>()
                .ForMember(d => d.Role, opt => opt.MapFrom(s => UserService.ResolveEffectiveRole(s.Role, s.LastIdpAssignedRole)));
            CreateMap<Data.User, TeamMember>();
            CreateMap<Data.User, ListUsersResponseUser>()
                .ForMember(d => d.AppRole, opt => opt.MapFrom(s => s.Role))
                .ForMember(d => d.EffectiveRole, opt => opt.MapFrom(s => UserService.ResolveEffectiveRole(s.Role, s.LastIdpAssignedRole)));
            CreateMap<Data.User, SimpleEntity>()
                .ForMember(s => s.Name, opt => opt.MapFrom(u => u.ApprovedName));

            // TO Data.User
            CreateMap<User, Data.User>();
            CreateMap<NewUser, Data.User>();
            CreateMap<UpdateUser, SelfChangedUser>();
            CreateMap<UpdateUser, Data.User>();
            CreateMap<SelfChangedUser, Data.User>();
        }
    }
}
