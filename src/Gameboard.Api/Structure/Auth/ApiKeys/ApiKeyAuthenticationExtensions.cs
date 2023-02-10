// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using Microsoft.AspNetCore.Authentication;
using Gameboard.Api.Auth;

namespace Microsoft.Extensions.DependencyInjection;

public static class ApiKeyAuthenticationExtensions
{
    public static AuthenticationBuilder AddApiKeyAuthentication(
        this AuthenticationBuilder builder,
        string scheme,
        Action<ApiKeyAuthenticationOptions> options
    ) => builder.AddScheme<ApiKeyAuthenticationOptions, ApiKeyAuthenticationHandler>
        (
            scheme ?? ApiKeyAuthentication.AuthenticationScheme,
            options
        );
}
