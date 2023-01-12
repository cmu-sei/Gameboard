using Gameboard.Api.Data;

namespace Gameboard.Tests.Integration.Fixtures;

public class GameBuilder
{
    public Action<Game>? Configure { get; set; }
    public bool WithChallengeSpec { get; set; } = false;
    public Action<ChallengeSpec>? ConfigureChallengeSpec { get; set; }

    public static GameBuilder WithConfig(Action<Game> configure)
    {
        return new GameBuilder { Configure = configure };
    }
}