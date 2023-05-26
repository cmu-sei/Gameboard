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
    public GbIntegrationAutoDataAttribute() : base(() => GbFixtureCustomizationFactory.Fixture) { }
}

public class GbIntegrationInlineAutoDataAttribute : InlineAutoDataAttribute
{
    public GbIntegrationInlineAutoDataAttribute(params object[] args) : base(new GbIntegrationAutoDataAttribute(), args) { }
}
