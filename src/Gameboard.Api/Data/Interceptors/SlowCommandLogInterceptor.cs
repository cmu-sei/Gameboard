using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Structure.Logging;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Gameboard.Api.Data;

public sealed class SlowCommandLogInterceptor(ILoggerFactory loggerFactory) : DbCommandInterceptor
{
    private static readonly int DURATION_THRESHOLD_MS = 1500;
    private readonly ILogger<SlowCommandLogInterceptor> _logger = loggerFactory.CreateLogger<SlowCommandLogInterceptor>();

    public override ValueTask<DbDataReader> ReaderExecutedAsync(DbCommand command, CommandExecutedEventData eventData, DbDataReader result, CancellationToken cancellationToken = default)
    {
        if (eventData.Duration.TotalMilliseconds >= DURATION_THRESHOLD_MS)
        {
            _logger.LogWarning(LogEventId.Db_LongRunningQuery, "Slow command ({durationMs}ms): {commandText}", eventData.Duration.TotalMilliseconds, command.CommandText);
        }

        return base.ReaderExecutedAsync(command, eventData, result, cancellationToken);
    }
}
