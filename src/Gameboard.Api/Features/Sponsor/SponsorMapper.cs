// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using AutoMapper;


namespace Gameboard.Api.Services
{
    public class SponsorMapper : Profile
    {
        public SponsorMapper()
        {
            CreateMap<string, string>().ConvertUsing(str => str == null ? null : str.Trim());
            CreateMap<Data.Sponsor, Sponsor>();
            CreateMap<Data.Sponsor, SponsorWithChildSponsors>();
            CreateMap<Sponsor, Data.Sponsor>();
            CreateMap<NewSponsor, Data.Sponsor>();
            CreateMap<UpdateSponsorRequest, Data.Sponsor>();
        }
    }
}
