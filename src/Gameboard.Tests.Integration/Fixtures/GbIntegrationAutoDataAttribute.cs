namespace Gameboard.Tests.Integration.Fixtures;

public class GbIntegrationAutoDataAttribute : AutoDataAttribute
{
    private static IFixture FIXTURE = new Fixture()
        .Customize(new GameboardCustomization());

    public GbIntegrationAutoDataAttribute() : base(() => FIXTURE) { }
}
