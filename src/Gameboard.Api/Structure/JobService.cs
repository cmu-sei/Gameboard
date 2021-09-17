// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Gameboard.Api
{
    public class JobService : IHostedService
    {
        private Timer _timer;
        private readonly ILogger _logger;
        private readonly IServiceProvider _services;

        public JobService(
            IServiceProvider serviceProvider
        )
        {
            _services = serviceProvider;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _timer = new Timer(RunTasks, null, 32000, 64000);

            return Task.FromResult(0);
        }

        private void RunTasks(object state)
        {
            using (var scope = _services.CreateScope())
            {
                try
                {
                    var svc = scope.ServiceProvider.GetRequiredService<ChallengeService>();
                    svc.SyncExpired().Wait();

                    var consoleMap = scope.ServiceProvider.GetService<ConsoleActorMap>();
                    consoleMap?.Prune();
                }
                catch {}
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(0);
        }
    }
}
