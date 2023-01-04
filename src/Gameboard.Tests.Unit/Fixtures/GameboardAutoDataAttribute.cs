namespace Gameboard.Tests.Unit.Fixtures;

public class GameboardAutoDataAttribute : AutoDataAttribute
{
    private static IFixture FIXTURE = new Fixture()
        .Customize(new AutoFakeItEasyCustomization());

    public GameboardAutoDataAttribute() : base(() => FIXTURE) { }
}