// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

namespace Gameboard.Api.Tests.Unit;

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
