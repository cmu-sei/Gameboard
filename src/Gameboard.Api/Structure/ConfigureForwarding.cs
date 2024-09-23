// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Linq;
using System.Net;
using Gameboard.Api;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ForwardingStartupExtension
    {

        public static IServiceCollection ConfigureForwarding(
            this IServiceCollection services,
            ForwardHeaderOptions options
        )
        {
            services.Configure<ForwardedHeadersOptions>(config =>
            {

                if (Enum.TryParse(
                    options.TargetHeaders ?? "None",
                    true,
                    out ForwardedHeaders targets)
                )
                {
                    config.ForwardedHeaders = targets;
                }

                config.ForwardLimit = options.ForwardLimit;

                if (options.ForwardLimit == 0)
                {
                    config.ForwardLimit = null;
                }

                string nets = options.KnownNetworks;
                if (string.IsNullOrEmpty(nets))
                    nets = "10.0.0.0/8 172.16.0.0/12 192.168.0.0/24 ::ffff:a00:0/104 ::ffff:ac10:0/108 ::ffff:c0a8:0/120";

                foreach (string item in nets.Split(new char[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    string[] net = item.Split('/');

                    if (IPAddress.TryParse(net.First(), out IPAddress ipaddr)
                        && int.TryParse(net.Last(), out int prefix)
                    )
                    {
                        config.KnownNetworks.Add(new AspNetCore.HttpOverrides.IPNetwork(ipaddr, prefix));
                    }
                }

                if (!string.IsNullOrEmpty(options.KnownProxies))
                {
                    foreach (string ip in options.KnownProxies.Split(new char[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        if (IPAddress.TryParse(ip, out IPAddress ipaddr))
                        {
                            config.KnownProxies.Add(ipaddr);
                        }
                    }
                }

            });

            return services;
        }
    }
}
