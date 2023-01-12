using Gameboard.Api;

namespace Gameboard.Tests.Unit;

public class CorsPolicyOptionsTests
{
    [Theory, GameboardAutoData]
    public void Build_WithStarAndOtherOrigins_AllowsAllOrigins(IFixture fixture)
    {
        // arrange
        var sut = fixture.Create<CorsPolicyOptions>();
        sut.Origins = new string[]
        {
            "*",
            fixture.Create<string>()
        };

        // act
        var policy = sut.Build();

        // assert
        policy.AllowAnyOrigin.ShouldBeTrue();
    }
}
