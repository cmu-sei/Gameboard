using System;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Structure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Gameboard.Api.Features.Games.External;

internal class ExternalGamePreDeployService : BackgroundService
{
    private readonly BackgroundTaskContext _backgroundTaskContext;
    private readonly ILogger<ExternalGamePreDeployService> _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IBackgroundTaskQueue _taskQueue;

    public ExternalGamePreDeployService
    (
        BackgroundTaskContext backgroundTaskContext,
        ILogger<ExternalGamePreDeployService> logger,
        IServiceScopeFactory serviceScopeFactory,
        IBackgroundTaskQueue taskQueue
    )
    {
        _backgroundTaskContext = backgroundTaskContext;
        _logger = logger;
        _serviceScopeFactory = serviceScopeFactory;
        _taskQueue = taskQueue;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation($"{nameof(ExternalGamePreDeployService)} is running...");
        return ProcessTaskQueueAsync(stoppingToken);
    }

    private async Task ProcessTaskQueueAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var workItem = await _taskQueue.DequeueAsync(stoppingToken);
                _logger.LogInformation($"{nameof(ExternalGamePreDeployService)} is executing a predeployment...");

                using var scope = _serviceScopeFactory.CreateScope();
                await workItem(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Prevent throwing if stoppingToken was signaled
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred executing a predeployment.");
            }
        }
    }

    public override async Task StopAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation($"{nameof(ExternalGamePreDeployService)} is stopping...");
        await base.StopAsync(stoppingToken);
    }
}
