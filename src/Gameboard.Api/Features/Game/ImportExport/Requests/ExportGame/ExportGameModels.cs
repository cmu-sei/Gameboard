namespace Gameboard.Api.Features.Games;

public sealed class ExportGamesResult
{
    public required GameImportExportBatch ExportBatch { get; set; }
}
