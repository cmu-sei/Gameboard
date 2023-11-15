using System;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Structure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nito.AsyncEx;

namespace Gameboard.Api.Features.Games.External;

internal class ExternalGamePreDeployService : BackgroundService
{
    private readonly BackgroundTaskContext _backgroundTaskContext;
    private readonly ILogger<ExternalGamePreDeployService> _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly AsyncLock _taskLock = new();
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
                using (await _taskLock.LockAsync(stoppingToken))
                {
                    var attemptGuid = Guid.NewGuid().ToString("n");
                    var workItem = await _taskQueue.DequeueAsync(stoppingToken);
                    _logger.LogInformation($"{nameof(ExternalGamePreDeployService)} is predeploying attempt {attemptGuid}...");

                    using var scope = _serviceScopeFactory.CreateScope();
                    await workItem(stoppingToken);
                    _logger.LogInformation($"{nameof(ExternalGamePreDeployService)} finished predeploying attempt {attemptGuid}.");
                }
            }
            catch (OperationCanceledException)
            {
                // Prevent throwing if stoppingToken was signaled
                _logger.LogInformation("Cancellation token sent.");
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
