// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using AutoMapper;

namespace Gameboard.Api.Services
{
    public class ChallengeGateMapper : Profile
    {
        public ChallengeGateMapper()
        {
            CreateMap<string, string>().ConvertUsing(str => str == null ? null : str.Trim());

            CreateMap<Data.ChallengeGate, ChallengeGate>();

            CreateMap<NewChallengeGate, Data.ChallengeGate>();

            CreateMap<ChangedChallengeGate, Data.ChallengeGate>();

        }
    }
}
