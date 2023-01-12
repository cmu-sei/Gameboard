using Gameboard.Api.Features.UnityGames;
using Gameboard.Tests.Unit.Fixtures;

namespace Gameboard.Tests.Unit;

public class UnityGameServiceTests
{
    [Theory, GameboardAutoData]
    public void GetMissionCompleteDefinitionString_Matches_MissionCompleteRegex(IFixture fixture)
    {
        // arrange
        var sut = fixture.Create<UnityGameService>();
        var regex = sut.GetMissionCompleteEventRegex();

        // act
        var missionCompleteString = sut.GetMissionCompleteDefinitionString(fixture.Create<string>());

        // assert
        missionCompleteString.ShouldMatch(regex.ToString());
    }
}
