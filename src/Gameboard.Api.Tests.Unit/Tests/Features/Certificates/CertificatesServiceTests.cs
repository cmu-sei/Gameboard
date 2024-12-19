using Gameboard.Api.Common.Services;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Certificates;
using Gameboard.Api.Features.Teams;

namespace Gameboard.Api.Tests.Unit;

public class CertificatesServiceTests
{
    private readonly CertificatesService _sut;

    public CertificatesServiceTests()
    {
        _sut = new CertificatesService
        (
            A.Fake<CoreOptions>(),
            A.Fake<INowService>(),
            A.Fake<IStore>(),
            A.Fake<ITeamService>()
        );
    }

    [Fact]
    public void GetDurationDescription_WithHoursAndMinutes_ResolvesExpected()
    {
        // given
        var duration = TimeSpan.FromMinutes(62);

        // when
        var result = _sut.GetDurationDescription(duration);

        // then
        result.ToLower().ShouldBe("1 hour and 2 minutes");
    }

    [Fact]
    public void GetDurationDescription_WithHours_HidesMinutes()
    {
        // given
        var duration = TimeSpan.FromHours(2);

        // when
        var result = _sut.GetDurationDescription(duration);

        // then
        result.ToLower().ShouldBe("2 hours");
    }

    [Fact]
    public void GetDurationDescription_WithMinutes_HidesHours()
    {
        // given
        var duration = TimeSpan.FromMinutes(39);

        // when
        var result = _sut.GetDurationDescription(duration);

        // then
        result.ToLower().ShouldBe("39 minutes");
    }

    [Fact]
    public void GetDurationDescription_WithHms_HidesSeconds()
    {
        // given
        // 1 hour, 3 minutes, 4 seconds
        var duration = TimeSpan.FromSeconds(3787);

        // when
        var result = _sut.GetDurationDescription(duration);

        // then
        result.ToLower().ShouldBe("1 hour and 3 minutes");
    }
}
