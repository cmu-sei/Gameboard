using Gameboard.Api.Structure;

namespace Gameboard.Api.Features.Games;

public record ImportGamesResponse(ImportedGame[] ImportedGames);

public sealed class InvalidImportPackage : GameboardValidationException
{
    public InvalidImportPackage() : base($"Your import package doesn't appear to be valid. Try extracting it to ensure that it has a manifest with at least one game in it.") { }
}
