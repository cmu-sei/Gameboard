// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;

namespace Gameboard.Api.Features.Games;

public sealed class ListExportBatchesResponse
{
    public required GameExportBatchView[] ExportBatches { get; set; }
}

public sealed class GameExportBatchView
{
    public required string Id { get; set; }
    public required SimpleEntity ExportedBy { get; set; }
    public required DateTimeOffset ExportedOn { get; set; }
    public required int GameCount { get; set; }
    public required string PackageDownloadUrl { get; set; }
}
