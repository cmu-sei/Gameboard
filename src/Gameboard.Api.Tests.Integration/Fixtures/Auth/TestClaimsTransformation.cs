// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

﻿using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;

namespace Gameboard.Api.Tests.Integration.Fixtures;

internal class TestClaimsTransformation : IClaimsTransformation
{
    public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
        => Task.FromResult(principal);
}
