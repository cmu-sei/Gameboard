// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

﻿using System;

namespace Gameboard.Api.Common.Services;

public interface IGuidService
{
    string Generate();
}

internal class GuidService : IGuidService
{
    // static so it can be referred to in places where we can't easily inject, like migrations
    public static string StaticGenerateGuid()
    {
        return Guid.NewGuid().ToString("n");
    }

    public string Generate()
    {
        return StaticGenerateGuid();
    }
}
