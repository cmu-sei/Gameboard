// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Gameboard.Api;

public class JobService : BackgroundService, IDisposable
{
    private int _runCount = 0;
    private readonly ILogger _logger;
    private readonly IServiceProvider _services;

    public JobService
    (
        ILogger<JobService> logger,
        IServiceProvider serviceProvider
    )
    {
        _logger = logger;
        _services = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            Interlocked.Increment(ref _runCount);
            _logger.LogInformation(message: $"Running the {nameof(JobService)} (this is run #{_runCount})...");
            using var scope = _services.CreateScope();

            var svc = scope.ServiceProvider.GetRequiredService<ChallengeService>();
            var consoleMap = scope.ServiceProvider.GetRequiredService<ConsoleActorMap>();

            try
            {
                await svc.SyncExpired();
                consoleMap.Prune();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error running the job service.");
            }

            _logger.LogInformation(message: $"{nameof(JobService)} run #{_runCount} complete. Waiting for next run...");
            await Task.Delay(TimeSpan.FromSeconds(60), stoppingToken);
        }
    }
}
