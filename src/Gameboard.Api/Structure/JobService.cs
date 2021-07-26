// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Gameboard.Api
{
    public class JobService : IHostedService
    {
        public IServiceProvider ServiceProvider { get; }

        public JobService(
            IServiceProvider serviceProvider
        )
        {
            ServiceProvider = serviceProvider;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            // using (var scope = ServiceProvider.CreateScope())
            // {
            //     var sourceSvc = scope.ServiceProvider.GetRequiredService<SourceService>();
            //     return sourceSvc.SyncAll();
            // }
            return Task.FromResult(0);

        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(0);
        }
    }
}
