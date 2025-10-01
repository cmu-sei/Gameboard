// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using AutoMapper;
using Gameboard.Api.Services;

namespace Gameboard.Api.Tests.Unit;

public class ChallengeMapperTests
{
    [Theory, GameboardAutoData]
    public void ChallengeMapper_WithChallengeEntity_MapsChallengePlayer(Api.Data.Challenge challenge)
    {
        // arrange
        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile(new ChallengeMapper());
        });
        var sut = new Mapper(mapperConfig);

        // act
        var result = sut.Map<ChallengeSummary>(challenge);

        // assert
        result.Players.First().Name.ShouldBe(challenge.Player.Name);
    }
}
