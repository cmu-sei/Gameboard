using Gameboard.Api.Tests.Shared;

namespace Gameboard.Tests.Unit.Fixtures;

public class GameboardAutoDataAttribute : AutoDataAttribute
{
    private static IFixture FIXTURE = new Fixture()
        .Customize(new AutoFakeItEasyCustomization())
        .Customize(new GameboardCustomization());

    public GameboardAutoDataAttribute() : base(() => FIXTURE) { }
}
