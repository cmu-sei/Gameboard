using Gameboard.Api.Data.Abstractions;
using Gameboard.Api.Features.Reports;
using Gameboard.Api.Services;

namespace Gameboard.Api.Tests.Unit;

public class SupportReportServiceTests
{

    [Fact]
    public void GetDateTimeSupportWindow_ForEachWindow_CorrectlyCalculates()
    {
        // can't InlineData these across three theories because they're not constant expressions
        // arrange
        var tenAMDate = new DateTimeOffset(new DateTime(2023, 5, 9, 10, 0, 0));
        var sevenPmDate = new DateTimeOffset(new DateTime(2023, 5, 9, 19, 49, 0));
        var oneAmDate = new DateTimeOffset(new DateTime(2023, 5, 9, 3, 0, 0));
        var sut = FakeBuilder.BuildMeA<SupportReportService>();

        // act
        var tenAmWindow = sut.GetTicketDateSupportWindow(tenAMDate);
        var sevenPmWindow = sut.GetTicketDateSupportWindow(sevenPmDate);
        var oneAmWindow = sut.GetTicketDateSupportWindow(oneAmDate);

        // assert
        tenAmWindow.ShouldBe(SupportReportTicketWindow.BusinessHours);
        sevenPmWindow.ShouldBe(SupportReportTicketWindow.EveningHours);
        oneAmWindow.ShouldBe(SupportReportTicketWindow.OffHours);
    }

    [Fact]
    public async Task QueryRecords_ByMinutesOpen_ExcludesNewerTickets()
    {
        // arrange
        // pretend it's 10:30am on 5/9/2023
        var now = A.Fake<INowService>();
        A.CallTo(() => now.Get()).Returns(new DateTimeOffset(new DateTime(2023, 5, 9, 10, 30, 0)));

        var tickets = new Data.Ticket[]
        {
            // this ticket was created at 8:58am on 5/9/2023, or about an hour and a half ago
            new Data.Ticket { Created = new DateTimeOffset(new DateTime(2023, 5, 9, 8, 58, 0)) },
            // this one was created 2 minutes ago
            new Data.Ticket { Created = new DateTimeOffset(new DateTime(2023, 5, 9, 10, 28, 0)) }
        }.BuildMock();

        var ticketStore = A.Fake<ITicketStore>();
        A.CallTo(() => ticketStore.ListWithNoTracking()).Returns(tickets);

        var parameters = new SupportReportParameters { MinutesSinceOpen = 60 };
        var sut = FakeBuilder.BuildMeA<SupportReportService>(now, ticketStore);

        // act
        var results = await sut.QueryRecords(parameters);

        // assert
        results.Count().ShouldBe(1);
    }
}
