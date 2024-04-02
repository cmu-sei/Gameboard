using System.Collections.Generic;

namespace Gameboard.Api.Data;

public sealed class ExternalGameHost : IEntity
{
    public string Id { get; set; }
    public required string Name { get; set; }
    public required string ClientUrl { get; set; }
    public required bool DestroyResourcesOnDeployFailure { get; set; }
    public int? GamespaceDeployBatchSize { get; set; }
    public string HostApiKey { get; set; }
    public required string HostUrl { get; set; }
    public string PingEndpoint { get; set; }
    public required string StartupEndpoint { get; set; }
    public string TeamExtendedEndpoint { get; set; }

    // nav properties
    public ICollection<Data.Game> UsedByGames { get; set; } = new List<Data.Game>();
}
