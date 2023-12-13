using Gameboard.Api.Common;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Reports;

namespace Gameboard.Api.Tests.Unit;

public class PlayersReportServiceTests
{
    [Theory, GameboardAutoData]
    public void GetQuery_WithPlayerAndTwoEnrollments_CalculatesLastPlayedCorrectly(IFixture fixture)
    {
        // given a user with two player records
        var targetDate = new DateTimeOffset(new DateTime(2023, 12, 13));
        var olderDate = new DateTimeOffset(new DateTime(2023, 12, 1));

        var users = new Data.User
        {
            Id = fixture.Create<string>(),
            Sponsor = fixture.Create<Data.Sponsor>(),
            CreatedOn = fixture.Create<DateTimeOffset>(),
            Enrollments = new Data.Player[]
            {
                new()
                {
                    Id = fixture.Create<string>(),
                    SessionBegin = olderDate,
                },
                new()
                {
                    Id = fixture.Create<string>(),
                    SessionBegin = olderDate
                }
            }
        }
        .ToEnumerable()
        .BuildMock();

        var fakeStore = A.Fake<IStore>();
        A
            .CallTo(() => fakeStore.WithNoTracking<Data.User>())
            .Returns(users);

        var sut = new PlayersReportService(fakeStore);

        // when we call with no parameters, basically
        var results = sut.GetQuery(new PlayersReportParameters());

        // we expect that the result uses the newest session begin as LastPlayedOn
        results.First().LastPlayedOn.ShouldBe(targetDate);
    }
}
