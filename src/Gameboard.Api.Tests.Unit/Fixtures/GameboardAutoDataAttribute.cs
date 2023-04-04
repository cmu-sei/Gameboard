using Gameboard.Api.Tests.Shared;

namespace Gameboard.Api.Tests.Unit.Fixtures;

public class GameboardAutoDataAttribute : AutoDataAttribute
{
    private static IFixture FIXTURE = new Fixture()
        .Customize(new AutoFakeItEasyCustomization())
        .Customize(new GameboardCustomization());

    public GameboardAutoDataAttribute() : base(() => FIXTURE) { }
}
