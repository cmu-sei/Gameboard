// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using AutoMapper;


namespace Gameboard.Api.Services
{
    public class ChallengeEventMapper : Profile
    {
        public ChallengeEventMapper()
        {
            CreateMap<string, string>().ConvertUsing(str => str == null ? null : str.Trim());

            CreateMap<Data.ChallengeEvent, ChallengeEvent>();

            CreateMap<ChallengeEvent, Data.ChallengeEvent>();

            CreateMap<NewChallengeEvent, Data.ChallengeEvent>();

            CreateMap<ChangedChallengeEvent, Data.ChallengeEvent>();
        }
    }
}
