using Gameboard.Api;
using Gameboard.Api.Common;
using Gameboard.Api.Data;

namespace Gameboard.Api.Tests.Integration;

public class SupportControllerTests(GameboardTestContext testContext) : IClassFixture<GameboardTestContext>
{
    private readonly GameboardTestContext _testContext = testContext;

    [Theory, GbIntegrationAutoData]
    public async Task Ticket_WhenCreatedWithAutoTagTrigger_AutoTags
    (
        IFixture fixture,
        string gameId,
        string tag,
        string sponsorId,
        string userId
    )
    {
        // given an autotag which triggers on sponsor and a player with that sponsor
        await _testContext.WithDataState(state =>
        {
            state.Add<Data.Game>(fixture, g =>
            {
                g.Id = gameId;
                g.Players = new Data.Player
                {
                    Id = fixture.Create<string>(),
                    Sponsor = state.Build<Data.Sponsor>(fixture, s => s.Id = sponsorId),
                    User = new Data.User { Id = userId, SponsorId = sponsorId }
                }.ToCollection();
            });

            state.Add<SupportSettingsAutoTag>(fixture, t =>
            {
                t.ConditionType = SupportSettingsAutoTagConditionType.SponsorId;
                t.ConditionValue = sponsorId;
                t.IsEnabled = true;
                t.Tag = tag;
            });
        });

        var result = await _testContext
            .CreateHttpClientWithAuthRole(UserRoleKey.Support)
            .PostAsync("api/ticket", new NewTicket
            {
                AssigneeId = userId,
                Description = fixture.Create<string>(),
                Summary = fixture.Create<string>(),
                RequesterId = userId,

            }
            .ToJsonBody())
            .DeserializeResponseAs<Ticket>();

        // TODO: test support for FromForm
    }
}
