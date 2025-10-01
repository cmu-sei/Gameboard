// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using Microsoft.AspNetCore.Authentication;

namespace Gameboard.Api.Tests.Integration.Fixtures;

internal class TestAuthenticationHandlerOptions : AuthenticationSchemeOptions
{
    public string DefaultUserId { get; set; } = TestAuthenticationUser.DEFAULT_USERID;
    public TestAuthenticationUser Actor { get; set; } = new TestAuthenticationUser();
}
