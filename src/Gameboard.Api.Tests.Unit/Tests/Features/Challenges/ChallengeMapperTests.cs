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
