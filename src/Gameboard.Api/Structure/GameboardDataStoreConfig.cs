using Microsoft.Extensions.Logging;

namespace Gameboard.Api.Structure;

internal class GameboardDataStoreConfig
{
    public required string ConnectionString { get; set; }
    public required bool EnableSensitiveDataLogging { get; set; }
    public required LogLevel MinimumLogLevel { get; set; }
}
