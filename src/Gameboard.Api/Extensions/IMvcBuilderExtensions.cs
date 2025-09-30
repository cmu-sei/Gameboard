// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

﻿using Gameboard.Api.Common.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Gameboard.Api.Extensions;

public static class IMvcBuilderExtensions
{
    internal static IMvcBuilder AddGameboardJsonOptions(this IMvcBuilder builder)
    {
        return builder.AddJsonOptions(jsonOptions =>
        {
            JsonService.BuildJsonSerializerOptions()(jsonOptions.JsonSerializerOptions);
        });
    }
}
