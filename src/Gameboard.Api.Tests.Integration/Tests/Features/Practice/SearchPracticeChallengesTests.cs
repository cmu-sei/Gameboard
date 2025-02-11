using Gameboard.Api.Common;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Practice;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Tests.Integration;

public class SearchPracticeChallengesTests(GameboardTestContext testContext) : IClassFixture<GameboardTestContext>
{
    private readonly GameboardTestContext _testContext = testContext;

    [Theory, GbIntegrationAutoData]
    public async Task SearchChallenges_WithTagMatchAndSuggestedSearch_Matches(string tag, IFixture fixture)
    {
        // given a challenge spec with a tag slug-equal to one of the suggested searches in 
        // the practice area...
        var dbContext = _testContext.GetValidationDbContext();
        var existingSettings = dbContext.Set<Data.PracticeModeSettings>();
        dbContext.Set<Data.PracticeModeSettings>().RemoveRange(existingSettings);
        await dbContext.SaveChangesAsync();

        await _testContext.WithDataState
        (
            state =>
            {
                // note that the original practice settings are established by migration, so we need to actually change
                // what's there rather than adding a new practice mode settings object
                state.Add<Data.PracticeModeSettings>(fixture, settings =>
                {
                    settings.SuggestedSearches = tag;
                });

                state.Add<Data.Game>(fixture, game =>
                {
                    game.IsPublished = true;
                    game.PlayerMode = PlayerMode.Practice;

                    game.Specs = state.Build<Data.ChallengeSpec>(fixture, spec =>
                    {
                        spec.Tags = tag;
                    }).ToCollection();
                });
            }
        );

        var settings1 = await dbContext.Set<Data.PracticeModeSettings>().ToListAsync();
        // when we search for the tag 
        var result = await _testContext
            .CreateClient()
            .GetAsync($"/api/practice?term={tag}")
            .DeserializeResponseAs<SearchPracticeChallengesResult>();

        // we should find it
        result.Results.Items.Count().ShouldBe(1);
    }

    [Theory, GbIntegrationAutoData]
    public async Task SearchChallenges_WithTagMatchAndWithoutSuggestedSearch_DoesNotMatch(string tag, IFixture fixture)
    {
        // given a challenge spec with a tag NOT slug-equal to one of the suggested searches in 
        // the practice area...
        await _testContext.WithDataState
        (
            state =>
            {
                // note there are no suggested searches in this db
                state.Add<Data.Game>(fixture, game =>
                {
                    game.PlayerMode = PlayerMode.Practice;
                    game.IsPublished = true;

                    game.Specs = state.Build<Data.ChallengeSpec>(fixture, spec =>
                    {
                        game.PlayerMode = PlayerMode.Practice;
                        spec.Tags = tag;
                    }).ToCollection();
                });
            }
        );

        // when we search for the tag 
        var result = await _testContext
            .CreateClient()
            .GetAsync($"/api/practice?term={tag}")
            .DeserializeResponseAs<SearchPracticeChallengesResult>();

        // we should find it
        result.Results.Items.Count().ShouldBe(0);
    }
}
