using Gameboard.Api.Structure;
using Microsoft.AspNetCore.Http;

namespace Gameboard.Api.Features.Games;

public sealed class ImportGamesRequest
{
    public string DelimitedGameIds { get; set; }
    public required IFormFile PackageFile { get; set; }
    public string SetGamesPublishStatus { get; set; }
}

public record ImportGamesResponse(ImportedGame[] ImportedGames);

public sealed class InvalidImportPackage : GameboardValidationException
{
    public InvalidImportPackage() : base($"Your import package doesn't appear to be valid. Try extracting it to ensure that it has a manifest with at least one game in it.") { }
}
