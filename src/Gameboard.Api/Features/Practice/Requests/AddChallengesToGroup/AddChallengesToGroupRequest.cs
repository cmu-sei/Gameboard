#nullable enable

namespace Gameboard.Api.Features.Practice;

public sealed class AddChallengesToGroupRequest
{
    public string? AddByGameId { get; set; }
    public string[]? AddBySpecIds { get; set; }
    public string? AddByTag { get; set; }
}
