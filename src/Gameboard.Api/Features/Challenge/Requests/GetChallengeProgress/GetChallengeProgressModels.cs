using Gameboard.Api.Features.GameEngine;

namespace Gameboard.Api.Features.Challenges;

public sealed class GetChallengeProgressResponse
{
    public required GameEngineChallengeProgressView Progress { get; set; }
}
