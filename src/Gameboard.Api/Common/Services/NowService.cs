// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;

namespace Gameboard.Api.Common.Services;

public interface INowService
{
    public DateTimeOffset Get();
}

internal class NowService : INowService
{
    public DateTimeOffset Get() => DateTimeOffset.UtcNow;
}
