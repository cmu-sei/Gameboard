// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using AutoMapper;
using Gameboard.Api.Data;

namespace Gameboard.Api.Features.Practice;

internal class PracticeMaps : Profile
{
    public PracticeMaps()
    {
        CreateMap<PracticeModeSettings, PracticeModeSettingsApiModel>()
            .ForMember(s => s.SuggestedSearches, o => o.Ignore());

        CreateMap<PracticeModeSettingsApiModel, PracticeModeSettings>()
            .ForMember(s => s.SuggestedSearches, o => o.Ignore());
    }
}
