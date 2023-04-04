using Gameboard.Api.Data;

namespace Gameboard.Api.Tests.Integration.Fixtures;

public class TeamBuilderOptions
{
    public required Action<Data.Game> GameBuilder { get; set; }
    public required string Name { get; set; }
    public required int NumPlayers { get; set; }
    public required string ChallengeId { get; set; }
    public required string ChallengeName { get; set; }
    public required string TeamId { get; set; }
}
