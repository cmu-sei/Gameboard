// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Collections.Generic;
using Gameboard.Api.Features.Games;

namespace Gameboard.Api.Data;

public sealed class ExternalGameHost : IEntity
{
    public string Id { get; set; }
    public required string Name { get; set; }
    public required string ClientUrl { get; set; }
    public required bool DestroyResourcesOnDeployFailure { get; set; }
    public int? GamespaceDeployBatchSize { get; set; }
    public int? HttpTimeoutInSeconds { get; set; }
    [DontExport]
    public string HostApiKey { get; set; }
    public required string HostUrl { get; set; }
    public string PingEndpoint { get; set; }
    public required string StartupEndpoint { get; set; }
    public string TeamExtendedEndpoint { get; set; }

    // nav properties
    [DontExport]
    public ICollection<Data.Game> UsedByGames { get; set; } = new List<Data.Game>();
}
