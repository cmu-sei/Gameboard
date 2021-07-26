// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using Gameboard.Api;
using Microsoft.AspNetCore.Authentication;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class TicketAuthenticationExtensions
    {
        public static AuthenticationBuilder AddTicketAuthentication(
            this AuthenticationBuilder builder,
            string scheme,
            Action<TicketAuthenticationOptions> options
        ) {

            builder.AddScheme<TicketAuthenticationOptions, TicketAuthenticationHandler>(
                scheme ?? TicketAuthentication.AuthenticationScheme,
                options
            );

            return builder;
        }
    }
}
