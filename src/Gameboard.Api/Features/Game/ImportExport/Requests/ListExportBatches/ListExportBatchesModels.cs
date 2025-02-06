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
