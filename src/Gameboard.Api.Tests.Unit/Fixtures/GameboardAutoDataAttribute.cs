using Gameboard.Api.Tests.Shared.Fixtures;

namespace Gameboard.Api.Tests.Unit.Fixtures;

public class GameboardAutoDataAttribute : AutoDataAttribute
{
    private static readonly IFixture FIXTURE = new Fixture()
        .Customize(new AutoFakeItEasyCustomization())
        .Customize(new GameboardCustomization());

    public GameboardAutoDataAttribute() : base(() => FIXTURE) { }
}
