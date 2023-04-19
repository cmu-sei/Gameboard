using Gameboard.Api.Tests.Shared;

namespace Gameboard.Api.Tests.Integration.Fixtures;

public class TeamBuilderOptions
{
    public required Action<Data.Game> GameBuilder { get; set; }
    public required string Name { get; set; }
    public required int NumPlayers { get; set; }
    public required SimpleEntity? Challenge { get; set; }
    public required string TeamId { get; set; }
}
