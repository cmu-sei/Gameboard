using Gameboard.Api.Data;

namespace Gameboard.Tests.Integration.Fixtures;

public class TeamBuilderOptions
{
    public required Action<Game> GameBuilder { get; set; }
    public required string Name { get; set; }
    public required int NumPlayers { get; set; }
    public required string TeamId { get; set; }
}
