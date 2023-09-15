using Gameboard.Api.Features.Teams;

namespace Gameboard.Api.Tests.Unit;

public class TeamsModelsTests
{
    [Theory, GameboardAutoData]
    public void TeamSummary_WithNullSponsorAndTeamSponsors_YieldsArrayEmptySponsorList(string teamId, string teamName)
    {
        // given 
        var teamSummary = new TeamSummary
        {
            Id = teamId,
            Name = teamName,
            Sponsor = null,
            TeamSponsors = null
        };

        // when
        var result = teamSummary.SponsorList;

        // then
        result.ShouldBe(Array.Empty<string>());
    }

    [Theory, GameboardAutoData]
    public void TeamSummary_WithNonNullTeamSponsors_YieldsExpectedArray
    (
        string teamId,
        string teamName,
        string sponsorId1,
        string sponsorId2
    )
    {
        // given 
        var teamSummary = new TeamSummary
        {
            Id = teamId,
            Name = teamName,
            Sponsor = null,
            TeamSponsors = $"{sponsorId1}|{sponsorId2}"
        };

        // when
        var result = teamSummary.SponsorList;

        // then
        result.ShouldBe(new string[] { sponsorId1, sponsorId2 });
    }
}
