using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Gameboard.Api.Common.Services;

/// <summary>
/// On rare occasions, we sometimes need processes to execute without forcing the response to a request
/// to wait. For example, when a sync-start game starts, the request from the last person to ready up
/// is the one that ultimately begins deployment of the game resources, but we don't want their
/// browser to indicate a timeout failure or whatever because we don't get back to them in time.
/// 
/// In situations like this, you can execute the work using this service. Use the provided parameter
/// `IServiceScope` to resolve services (because you don't want the app to try to reuse non-thread-
/// safe resources like a DbContext). DO NOT use non-thread-safe services in the work you pass in,
/// because the code will execute in parallel.
/// 
/// Last, if you use this, be sure you're aware that the response will be sent, but the work you're 
/// queueing up won't be done before it is.
/// </summary>
public interface IFireAndForgetService
{
    void Fire<T>(Func<T, Task> doWork);
}

internal class FireAndForgetService : IFireAndForgetService
{
    private readonly ILogger<FireAndForgetService> _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public FireAndForgetService(ILogger<FireAndForgetService> logger, IServiceScopeFactory serviceScopeFactory)
    {
        _logger = logger;
        _serviceScopeFactory = serviceScopeFactory;
    }

    public void Fire<T>(Func<T, Task> doWork)
    {
        Task.Run(async () =>
        {
            try
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var thing = scope.ServiceProvider.GetRequiredService<T>();
                await doWork(thing);
            }
            catch (Exception ex)
            {
                _logger.LogError($"FireAndForgetService exception: {ex.GetType().Name} :: {ex.Message}", ex);
            }
        });
    }
}
