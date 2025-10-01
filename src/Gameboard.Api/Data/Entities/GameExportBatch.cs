// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;

namespace Gameboard.Api.Data;

public class GameExportBatch : IEntity
{
    public string Id { get; set; }
    public required DateTimeOffset ExportedOn { get; set; }

    // navs
    public User ExportedByUser { get; set; }
    public required string ExportedByUserId { get; set; }
    public required ICollection<Game> IncludedGames { get; set; }
}
