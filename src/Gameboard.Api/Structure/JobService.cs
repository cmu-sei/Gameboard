// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Features.Challenges;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Gameboard.Api;

public class JobService(
    ILogger<JobService> logger,
    IServiceProvider serviceProvider
    ) : BackgroundService, IDisposable, IAsyncDisposable
{
    private int _runCount = 0;
    private readonly ILogger _logger = logger;
    private readonly IServiceProvider _services = serviceProvider;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            Interlocked.Increment(ref _runCount);
            _logger.LogInformation(message: $"Running the {nameof(JobService)} (this is run #{_runCount})...");
            using var scope = _services.CreateScope();

            var svc = scope.ServiceProvider.GetRequiredService<IChallengeSyncService>();

            try
            {
                await svc.SyncExpired(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error running the job service.");
            }

            _logger.LogInformation(message: $"{nameof(JobService)} run #{_runCount} complete. Waiting for next run...");
            await Task.Delay(TimeSpan.FromSeconds(60), stoppingToken);
        }
    }

    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }
}
