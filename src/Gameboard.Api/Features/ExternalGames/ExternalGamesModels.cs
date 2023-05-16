using Gameboard.Api.Structure;

namespace Gameboard.Api.Features.ExternalGames;

public sealed class ExternalGameMetaData
{
    public required SimpleEntity Game { get; set; }
}
