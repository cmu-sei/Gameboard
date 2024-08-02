using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Gameboard.Api.Data;

public sealed class SlowCommandLogInterceptor : DbCommandInterceptor
{
    private static readonly int DURATION_THRESHOLD_MS = 1500;
    private readonly ILogger<SlowCommandLogInterceptor> _logger;

    public SlowCommandLogInterceptor(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<SlowCommandLogInterceptor>();
    }

    public override ValueTask<DbDataReader> ReaderExecutedAsync(DbCommand command, CommandExecutedEventData eventData, DbDataReader result, CancellationToken cancellationToken = default)
    {
        if (eventData.Duration.TotalMilliseconds >= DURATION_THRESHOLD_MS)
        {
            _logger.LogWarning($"Slow command ({eventData.Duration.TotalMilliseconds} ms): {command.CommandText}");
        }

        return base.ReaderExecutedAsync(command, eventData, result, cancellationToken);
    }
}
