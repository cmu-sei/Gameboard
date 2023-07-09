using Gameboard.Api.Tests.Shared.Fixtures;

namespace Gameboard.Api.Tests.Integration.Fixtures;

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

public class GbIntegrationInlineAutoDataAttribute : CompositeDataAttribute
{
    public GbIntegrationInlineAutoDataAttribute(params object[] fixedValues)
        : base(new InlineAutoDataAttribute(fixedValues), new GbIntegrationAutoDataAttribute()) { }
}
