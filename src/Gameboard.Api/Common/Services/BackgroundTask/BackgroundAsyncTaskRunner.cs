using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nito.AsyncEx;

namespace Gameboard.Api.Common.Services;

internal class BackgroundAsyncTaskRunner
 : BackgroundService
{
    private readonly BackgroundAsyncTaskContext _backgroundTaskContext;
    private readonly ILogger<BackgroundAsyncTaskRunner> _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly AsyncLock _taskLock = new();
    private readonly IBackgroundAsyncTaskQueueService _taskQueue;

    public BackgroundAsyncTaskRunner
    (
        BackgroundAsyncTaskContext backgroundTaskContext,
        ILogger<BackgroundAsyncTaskRunner> logger,
        IServiceScopeFactory serviceScopeFactory,
        IBackgroundAsyncTaskQueueService taskQueue
    )
    {
        _backgroundTaskContext = backgroundTaskContext;
        _logger = logger;
        _serviceScopeFactory = serviceScopeFactory;
        _taskQueue = taskQueue;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation($"{nameof(BackgroundAsyncTaskRunner)} is running...");
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
                    var attemptGuid = GuidService.StaticGenerateGuid();
                    var workItem = await _taskQueue.DequeueAsync(stoppingToken);
                    _logger.LogInformation($"{nameof(BackgroundAsyncTaskRunner)} is working async task {attemptGuid}...");

                    using var scope = _serviceScopeFactory.CreateScope();
                    await workItem(stoppingToken);
                    _logger.LogInformation($"{nameof(BackgroundAsyncTaskRunner)} finished working async task {attemptGuid}.");
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
        _logger.LogInformation($"{nameof(BackgroundAsyncTaskRunner)} is stopping...");
        await base.StopAsync(stoppingToken);
    }
}
