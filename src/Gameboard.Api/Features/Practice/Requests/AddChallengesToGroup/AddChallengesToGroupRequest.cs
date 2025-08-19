#nullable enable

namespace Gameboard.Api.Features.Practice;

public sealed class AddChallengesToGroupRequest
{
    public string? AddByGameId { get; set; }
    public string? AddByGameDivision { get; set; }
    public string? AddByGameSeason { get; set; }
    public string? AddByGameSeries { get; set; }
    public string? AddByGameTrack { get; set; }
    public string[]? AddBySpecIds { get; set; }
    public string? AddByTag { get; set; }
}
