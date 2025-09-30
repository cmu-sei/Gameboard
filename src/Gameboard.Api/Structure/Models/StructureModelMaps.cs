// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using AutoMapper;
using Gameboard.Api.Common;

namespace Gameboard.Api.Structure;

internal class StructureModelMaps : Profile
{
    public StructureModelMaps()
    {
        CreateMap<SimpleEntity, string>()
            .ConvertUsing(se => se != null ? se.Name : string.Empty);
    }
}
