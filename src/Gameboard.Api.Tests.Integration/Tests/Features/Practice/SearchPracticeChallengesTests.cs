using Gameboard.Api.Common;
using Gameboard.Api.Features.Practice;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Tests.Integration;

[Collection(TestCollectionNames.DbFixtureTests)]
public class SearchPracticeChallengesTests
{
    private readonly GameboardTestContext _testContext;

    public SearchPracticeChallengesTests(GameboardTestContext testContext)
        => _testContext = testContext;

    [Theory, GbIntegrationAutoData]
    public async Task SearchChallenges_WithTagMatchAndSuggestedSearch_Matches(string tag, IFixture fixture)
    {
        // given a challenge spec with a tag slug-equal to one of the suggested searches in 
        // the practice area...
        var existingSettings = _testContext.GetDbContext().Set<Data.PracticeModeSettings>();
        _testContext.GetDbContext().Set<Data.PracticeModeSettings>().RemoveRange(existingSettings);
        await _testContext.GetDbContext().SaveChangesAsync();

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
                    game.Specs = state.Build<Data.ChallengeSpec>(fixture, spec =>
                    {
                        game.PlayerMode = PlayerMode.Practice;
                        spec.Tags = tag;
                    }).ToCollection();
                });
            }
        );

        var settings1 = await _testContext.GetDbContext().Set<Data.PracticeModeSettings>().ToListAsync();
        // when we search for the tag 
        var result = await _testContext
            .CreateClient()
            .GetAsync($"/api/practice?term={tag}")
            .WithContentDeserializedAs<SearchPracticeChallengesResult>();

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
            .WithContentDeserializedAs<SearchPracticeChallengesResult>();

        // we should find it
        result.Results.Items.Count().ShouldBe(0);
    }
}
