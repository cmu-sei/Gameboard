using Gameboard.Api.Features.GameEngine;

namespace Gameboard.Api.Features.Challenges;

public sealed class GetChallengeProgressResponse
{
    public required SimpleEntity Spec { get; set; }
    public required SimpleEntity Team { get; set; }
    public required GameEngineChallengeProgressView Progress { get; set; }
}
