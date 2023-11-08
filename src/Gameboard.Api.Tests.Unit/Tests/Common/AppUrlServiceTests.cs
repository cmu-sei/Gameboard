using Gameboard.Api.Common;
using Gameboard.Api.Common.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;

namespace Gameboard.Api.Tests.Unit;

public class AppUrlServiceTests
{
    [Fact]
    public void GetBaseUrl_WithFixedRequest_BuildsExpected()
    {
        // given
        var addressesFeature = A.Fake<IServerAddressesFeature>();
        A.CallTo(() => addressesFeature.Addresses)
            .Returns("https://gameboard.com/test/gb".ToCollection());

        var hostingServer = A.Fake<IServer>();
        A.CallTo(() => hostingServer.Features.Get<IServerAddressesFeature>())
            .Returns(addressesFeature);


        // (sut)
        var sut = new AppUrlService(hostingServer);

        // when
        var result = sut.GetBaseUrl();

        // then
        result.ShouldBe("https://gameboard.com/test/gb");
    }

    [Fact]
    public void GetAbsoluteUrlFromRelative_WithFixedRequest_BuildsExpected()
    {
        // given
        var addressesFeature = A.Fake<IServerAddressesFeature>();
        A.CallTo(() => addressesFeature.Addresses)
            .Returns("https://gameboard.com/test/gb".ToCollection());

        var hostingServer = A.Fake<IServer>();
        A.CallTo(() => hostingServer.Features.Get<IServerAddressesFeature>())
            .Returns(addressesFeature);


        // (sut)
        var sut = new AppUrlService(hostingServer);

        // when
        var result = sut.ToAppAbsoluteUrl("mks");

        // then
        result.ShouldBe("https://gameboard.com/test/gb/mks");
    }
}
