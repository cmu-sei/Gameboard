using Gameboard.Api.Tests.Shared.Fixtures;

namespace Gameboard.Api.Tests.Integration.Fixtures;

file static class GbFixtureCustomizationFactory
{
    public static IFixture Fixture
    {
        get => new Fixture().Customize(new GameboardCustomization());
    }
}

public class GbIntegrationAutoDataAttribute : AutoDataAttribute
{
    private static readonly IFixture FIXTURE = new Fixture()
        .Customize(new GameboardCustomization());

    public GbIntegrationAutoDataAttribute() : base(() =>
    {
        FIXTURE.Customizations.Add(new IdBuilder());
        return FIXTURE;
    })
    { }
}
